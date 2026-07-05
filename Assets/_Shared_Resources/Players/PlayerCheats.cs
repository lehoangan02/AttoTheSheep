using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public enum CheatBuffType
{
    ShieldAllLambs,
    SpawnMaxLambs,
    SkillDamageMultiplier,
    SpeedBoostPlayerAndFlock
}

[Serializable]
public class CheatBuffConfig
{
    public CheatBuffType buffType = CheatBuffType.ShieldAllLambs;

    [Tooltip("Multiplier value for damage/speed buffs. Ignored by spawn and shield.")]
    public float value = 1.25f;

    [Min(0f)]
    [Tooltip("Duration in seconds. Ignored by SpawnMaxLambs.")]
    public float duration = 8f;

    [Min(0f)]
    [Tooltip("Extra value for specific buffs (for speed boost = acceleration duration).")]
    public float extraValue = 0.5f;
}

[Serializable]
public class CheatSlotConfig
{
    [Range(1, 9)] public int cheatId = 1;
    public string cheatName = "Cheat";
    public List<CheatBuffConfig> buffs = new List<CheatBuffConfig>();
}

/// <summary>
/// Applies cheat effects by slot id. Each slot can contain multiple buffs
/// and each buff can define its own value and duration.
/// </summary>
public class PlayerCheats : NetworkBehaviour
{
    private PlayerController controller;
    private PlayerMovement playerMovement;
    private FlockManager flockManager;
    private PlayerSkills playerSkills;
    private PlayerEntity playerEntity;

    [Header("Player Cheat Particle FX")]
    [SerializeField] private ParticleSystem damageAuraParticles;
    [SerializeField] private ParticleSystem speedWindParticles;

    [Header("Activation FX (Glow & Shockwave)")]
    [SerializeField] private ParticleSystem shockwaveParticles;
    [Tooltip("Kéo Renderer của nhân vật vào đây (SpriteRenderer, MeshRenderer hoặc SkinnedMeshRenderer)")]
    [SerializeField] private Renderer[] playerRenderers;
    [Tooltip("Màu chớp sáng (Nên bật HDR để có hiệu ứng Glow/Bloom)")]
    [ColorUsage(true, true)] [SerializeField] private Color flashColor = new Color(2f, 2f, 2f, 1f);
    [SerializeField] private float flashDuration = 0.2f; // Tăng nhẹ một chút để thấy rõ nhịp đập

    [Tooltip("Kéo Object chứa đồ họa/hình ảnh của nhân vật vào đây để phóng to thu nhỏ mà không lỗi vật lý.")]
    [SerializeField] private Transform visualTransform;
    [Tooltip("Độ phóng to tối đa khi kích hoạt (Ví dụ: 1.25 là phóng to thêm 25%)")]
    [SerializeField] private float pulseScaleMultiplier = 1.25f;

    // Lưu trữ Coroutine để tránh việc spam cheat làm tắt hạt sai thời điểm
    private Coroutine damageAuraCoroutine;
    private Coroutine speedWindCoroutine;
    private Coroutine flashCoroutine;

    [Header("Cheat Slots")]
    [SerializeField] private List<CheatSlotConfig> cheatSlots = new List<CheatSlotConfig>
    {
        new CheatSlotConfig
        {
            cheatId = 1,
            cheatName = "Flock Shield",
            buffs = new List<CheatBuffConfig>
            {
                new CheatBuffConfig
                {
                    buffType = CheatBuffType.ShieldAllLambs,
                    duration = 10f
                }
            }
        },
        new CheatSlotConfig
        {
            cheatId = 2,
            cheatName = "Spawn Max Lambs",
            buffs = new List<CheatBuffConfig>
            {
                new CheatBuffConfig
                {
                    buffType = CheatBuffType.SpawnMaxLambs
                }
            }
        },
        new CheatSlotConfig
        {
            cheatId = 3,
            cheatName = "Skill Damage Boost",
            buffs = new List<CheatBuffConfig>
            {
                new CheatBuffConfig
                {
                    buffType = CheatBuffType.SkillDamageMultiplier,
                    value = 1.25f
                }
            }
        },
        new CheatSlotConfig
        {
            cheatId = 4,
            cheatName = "Speed Boost",
            buffs = new List<CheatBuffConfig>
            {
                new CheatBuffConfig
                {
                    buffType = CheatBuffType.SpeedBoostPlayerAndFlock,
                    value = 1.5f,
                    duration = 8f,
                    extraValue = 0.5f
                }
            }
        }
    };

    void Awake()
    {
        ResolveReferences();
    }

    public override void OnNetworkSpawn()
    {
        ResolveReferences();
        if (IsOwner && controller != null)
        {
            controller.OnCheatActivated += HandleCheatActivated;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (controller != null)
        {
            controller.OnCheatActivated -= HandleCheatActivated;
        }
    }

    private void ResolveReferences()
    {
        controller = GetComponent<PlayerController>();
        if (controller == null) controller = GetComponentInChildren<PlayerController>();
        if (controller == null) controller = GetComponentInParent<PlayerController>();

        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null) playerMovement = GetComponentInChildren<PlayerMovement>();
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();

        playerSkills = GetComponent<PlayerSkills>();
        if (playerSkills == null) playerSkills = GetComponentInChildren<PlayerSkills>();
        if (playerSkills == null) playerSkills = GetComponentInParent<PlayerSkills>();

        playerEntity = GetComponent<PlayerEntity>();
        if (playerEntity == null) playerEntity = GetComponentInChildren<PlayerEntity>();
        if (playerEntity == null) playerEntity = GetComponentInParent<PlayerEntity>();
    }

    private FlockManager GetFlockManager()
    {
        if (flockManager == null)
        {
            flockManager = FindFirstObjectByType<FlockManager>();
        }

        return flockManager;
    }

    private CheatSlotConfig FindSlot(int cheatId)
    {
        return cheatSlots.Find(slot => slot != null && slot.cheatId == cheatId);
    }

    private void HandleCheatActivated(int cheatId)
    {
        if (!IsOwner) return;

        CheatSlotConfig slot = FindSlot(cheatId);
        if (slot == null)
        {
            Debug.LogWarning($"[Cheats] No slot configured for Cheat ID {cheatId}.");
            return;
        }

        ApplyCheatSlotServerRpc(cheatId);
    }

    [ServerRpc]
    private void ApplyCheatSlotServerRpc(int cheatId)
    {
        ResolveReferences();

        CheatSlotConfig slot = FindSlot(cheatId);
        if (slot == null)
        {
            Debug.LogWarning($"[Cheats] Server missing slot config for Cheat ID {cheatId}.");
            return;
        }

        if (slot.buffs == null || slot.buffs.Count == 0)
        {
            Debug.LogWarning($"[Cheats] Slot '{slot.cheatName}' has no buffs configured.");
            return;
        }

        foreach (CheatBuffConfig buff in slot.buffs)
        {
            if (buff == null) continue;
            ApplyBuff(buff);
        }
    }

    private void ApplyBuff(CheatBuffConfig buff)
    {
        switch (buff.buffType)
        {
            case CheatBuffType.ShieldAllLambs:
                ApplyShieldAllLambs(buff.duration);
                break;

            case CheatBuffType.SpawnMaxLambs:
                ApplySpawnMaxLambs();
                break;

            case CheatBuffType.SkillDamageMultiplier:
                ApplySkillDamageMultiplier(buff.value, buff.duration);
                break;

            case CheatBuffType.SpeedBoostPlayerAndFlock:
                ApplySpeedBoost(buff.value, buff.duration, buff.extraValue);
                break;
        }
    }

    private void ApplyShieldAllLambs(float duration)
    {
        FlockManager fm = GetFlockManager();
        if (fm == null) return;

        float finalDuration = Mathf.Max(0f, duration);
        foreach (LambAI lamb in fm.activeLambs)
        {
            if (lamb != null && lamb.gameObject.activeInHierarchy)
            {
                lamb.SetShieldedState(true, finalDuration);
            }
        }
    }

    private void ApplySpawnMaxLambs()
    {
        FlockManager fm = GetFlockManager();
        if (fm == null) return;

        int maxLambs = fm.GetCurrentLevelConfig().maxLambs;
        int currentCount = fm.activeLambs.Count;

        for (int i = currentCount; i < maxLambs; i++)
        {
            LambAI spawnedLamb = fm.SpawnLamb(fm.currentFlockCenter.Value);
            if (spawnedLamb != null)
            {
                spawnedLamb.PlayReviveSpawnFx();
            }
        }
    }

    private void ApplySkillDamageMultiplier(float multiplier, float duration)
    {
        if (playerSkills == null) return;

        float finalMultiplier = Mathf.Max(0f, multiplier);
        float finalDuration = Mathf.Max(0f, duration);
        
        playerSkills.damageMultiplier.Value = finalMultiplier;

        PlayDamageAuraClientRpc(finalDuration);
        PlayActivationFXClientRpc(); 
    }

    private void ApplySpeedBoost(float multiplier, float duration, float accelerationDuration)
    {
        float finalMultiplier = Mathf.Max(0f, multiplier);
        float finalDuration = Mathf.Max(0f, duration);
        float finalAccelerationDuration = Mathf.Max(0f, accelerationDuration);

        if (playerMovement != null)
        {
            playerMovement.ApplyTemporarySpeedMultiplier(finalMultiplier, finalDuration, finalAccelerationDuration);
        }

        PlaySpeedWindClientRpc(finalDuration);
        PlayActivationFXClientRpc(); 

        FlockManager fm = GetFlockManager();
        if (fm != null)
        {
            foreach (LambAI lamb in fm.activeLambs)
            {
                if (lamb != null && lamb.gameObject.activeInHierarchy)
                {
                    lamb.ApplySpeedBoost(finalMultiplier, finalDuration);
                }
            }
        }
    }

    // =======================================================================
    // CLIENT RPCs & EFFECTS
    // =======================================================================

    [ClientRpc]
    private void PlayActivationFXClientRpc()
    {
        // 1. Phát hạt sóng xung kích
        if (shockwaveParticles != null)
        {
            shockwaveParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            shockwaveParticles.Play(true);
        }

        // 2. Chớp nháy nhân vật kết hợp Co Giãn (Glow Up + Pulse Scale)
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashAndPulseRoutine());
    }

    [ClientRpc]
    private void PlayDamageAuraClientRpc(float duration)
    {
        if (damageAuraParticles == null) return;

        damageAuraParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        damageAuraParticles.Play(true);

        if (damageAuraCoroutine != null) StopCoroutine(damageAuraCoroutine);
        if (duration > 0f)
        {
            damageAuraCoroutine = StartCoroutine(StopParticleAfterDelay(damageAuraParticles, duration));
        }
    }

    [ClientRpc]
    private void PlaySpeedWindClientRpc(float duration)
    {
        if (speedWindParticles == null) return;

        var main = speedWindParticles.main;
        main.duration = Mathf.Max(0.05f, duration);
        speedWindParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        speedWindParticles.Play(true);

        if (speedWindCoroutine != null) StopCoroutine(speedWindCoroutine);
        if (duration > 0f)
        {
            speedWindCoroutine = StartCoroutine(StopParticleAfterDelay(speedWindParticles, duration));
        }
    }

    // =======================================================================
    // COROUTINES
    // =======================================================================

    private IEnumerator FlashAndPulseRoutine()
    {
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        
        // Xác định đối tượng cần Scale (Ưu tiên visualTransform, nếu trống thì dùng chính nó)
        Transform targetTransform = visualTransform != null ? visualTransform : transform;
        Vector3 originalScale = targetTransform.localScale;

        float elapsed = 0f;

        // Bắt đầu áp màu chớp sáng lên tất cả Renderer
        foreach (var r in playerRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", flashColor);
            propBlock.SetColor("_BaseColor", flashColor);
            propBlock.SetColor("_EmissionColor", flashColor); 
            r.SetPropertyBlock(propBlock);
        }

        // Vòng lặp nội suy mượt mà hiệu ứng tim đập (Pulse Scale)
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / flashDuration;

            // Sử dụng hàm Sin từ 0 -> PI để tạo đồ thị hình parabol (0 tăng lên 1 rồi hạ xuống 0)
            float pulseCurve = Mathf.Sin(pct * Mathf.PI);

            // Nội suy kích thước dựa trên nhịp đập hình Sin
            targetTransform.localScale = originalScale * Mathf.Lerp(1f, pulseScaleMultiplier, pulseCurve);

            yield return null;
        }

        // Đảm bảo trả kích thước về chuẩn xác ban đầu
        targetTransform.localScale = originalScale;

        // Khôi phục lại trạng thái vật liệu ban đầu (Xóa màu flash)
        foreach (var r in playerRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(propBlock);
            propBlock.Clear(); 
            r.SetPropertyBlock(propBlock);
        }
    }

    private IEnumerator StopParticleAfterDelay(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}