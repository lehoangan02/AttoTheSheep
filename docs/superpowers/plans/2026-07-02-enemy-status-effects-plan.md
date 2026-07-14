# Enemy Status Effects & Family Data Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add status effects to Skeleton, Spider, Snake, and Wizard attacks, move knight guard chances into data assets, and wire the new data into existing enemy prefabs.

**Architecture:** Introduce two small `ScriptableObject` subclasses (`KnightEnemyData`, `CaveEnemyData`) that extend `MeleeEnemyData`, extend `WizardEnemyData` for throw effects, add `Bleed` to `EffectKind`, and update the corresponding brain scripts to read from the new data. Data assets are hand-edited to point to the new class GUIDs while preserving their existing GUIDs so prefab references remain valid.

**Tech Stack:** Unity 2022+, C#, Netcode for GameObjects, YAML asset editing.

## Global Constraints
- Do not modify base `EnemyData` or `MeleeEnemyData`.
- Do not add status effects to Troll attacks.
- Keep existing data asset GUIDs so prefab references stay intact.
- All enemy status-effect application stays server-authoritative (existing pattern).
- Use `EffectKind.Bleed` for Skeleton; extend the enum to add it.

---

## Task 1: Extend `EffectKind` with `Bleed`

**Files:**
- Modify: `Assets/_Shared_Resources/Effects/StatusEffectData.cs`

**Interfaces:**
- Produces: `EffectKind.Bleed` enum value.

- [ ] **Step 1: Add `Bleed` to the enum**

```csharp
public enum EffectKind { Poison, Burn, Freeze, Stun, Knockback, Slow, Bleed }
```

- [ ] **Step 2: Verify no switches break**

Search `StatusEffectController.cs` and any other `EffectKind` consumers. Only `HasEffect` has client approximations for `Freeze`/`Stun`/`Slow`; `Bleed` is handled generically.

- [ ] **Step 3: Verify project compiles**

Open Unity or trigger a script compilation and confirm no errors.

---

## Task 2: Create `KnightEnemyData`

**Files:**
- Create: `Assets/_Shared_Resources/Enemies/Data/KnightEnemyData.cs`

**Interfaces:**
- Produces: `KnightEnemyData : MeleeEnemyData` with `guardChance`.
- Consumed by: `BlackKnightBrain`, `BlueKnightBrain` (Task 5).

- [ ] **Step 1: Create the script**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "KnightEnemyData", menuName = "Gameplay/Enemies/Knight Enemy Data")]
public class KnightEnemyData : MeleeEnemyData
{
    [Header("Guard")]
    [Range(0f, 1f)]
    public float guardChance = 0.2f;
}
```

- [ ] **Step 2: Create the `.meta` file**

```yaml
fileFormatVersion: 2
guid: 23612c8a809e496e8566ac285d01eaa4
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

---

## Task 3: Create `CaveEnemyData`

**Files:**
- Create: `Assets/_Shared_Resources/Enemies/Data/CaveEnemyData.cs`

**Interfaces:**
- Produces: `CaveEnemyData : MeleeEnemyData` with `StatusEffectData[] onHitEffects`.
- Consumed by: `SkeletonBrain`, `SpiderBrain`, `SnakeBrain` (Task 6).

- [ ] **Step 1: Create the script**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "CaveEnemyData", menuName = "Gameplay/Enemies/Cave Enemy Data")]
public class CaveEnemyData : MeleeEnemyData
{
    [Header("Status Effects")]
    [Tooltip("Status effects applied on a successful melee hit.")]
    public StatusEffectData[] onHitEffects;
}
```

- [ ] **Step 2: Create the `.meta` file**

```yaml
fileFormatVersion: 2
guid: bee7f887eb1948eea21972ed80405273
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

---

## Task 4: Extend `WizardEnemyData`

**Files:**
- Modify: `Assets/_Shared_Resources/Enemies/Data/WizardEnemyData.cs`

**Interfaces:**
- Produces: `WizardEnemyData.throwEffects` array.
- Consumed by: `WizardBrain` (Task 7).

- [ ] **Step 1: Add `throwEffects` field**

Insert after the `pigPrefab` field:

```csharp
    [Header("Throw Status Effects")]
    public StatusEffectData[] throwEffects;
```

- [ ] **Step 2: Verify the full file**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "WizardEnemyData", menuName = "Gameplay/Enemies/Wizard Enemy Data")]
public class WizardEnemyData : EnemyData
{
    [Header("Throw")]
    public float throwRange = 8f;
    public float throwCooldown = 1.5f;
    public int throwDamage = 40;
    public float ballSpeed = 7f;
    public GameObject explosionBallPrefab;

    [Header("Transform")]
    public float transformRange = 3f;
    public float transformCooldown = 8f;
    public GameObject transformSpellVFXPrefab;
    public GameObject pigPrefab;

    [Header("Throw Status Effects")]
    public StatusEffectData[] throwEffects;
}
```

---

## Task 5: Update Knight Brains to Use `KnightEnemyData`

**Files:**
- Modify: `Assets/_Shared_Resources/Enemies/AI/BlackKnightBrain.cs`
- Modify: `Assets/_Shared_Resources/Enemies/AI/BlueKnightBrain.cs`

**Interfaces:**
- Consumes: `KnightEnemyData.guardChance`, `KnightEnemyData.attackDamage`, `KnightEnemyData.attackRange`.

### BlackKnightBrain.cs

- [ ] **Step 1: Replace `meleeData` with `knightData`**

Old:
```csharp
    [SerializeField] float guardChance = 0.3f;
    bool isLeftAttack = true;
    [SerializeField] private EnemyHitbox leftHitbox;
    [SerializeField] private EnemyHitbox rightHitbox;
    private MeleeEnemyData meleeData;
```

New:
```csharp
    bool isLeftAttack = true;
    [SerializeField] private EnemyHitbox leftHitbox;
    [SerializeField] private EnemyHitbox rightHitbox;
    private KnightEnemyData knightData;
```

- [ ] **Step 2: Update `Awake()` data load**

Old:
```csharp
        meleeData = entity.GetData<MeleeEnemyData>();
```

New:
```csharp
        knightData = entity.GetData<KnightEnemyData>();
```

- [ ] **Step 3: Update `DecideNextState()` references**

Old:
```csharp
        if (dist > meleeData.attackRange) { SetState(EnemyState.Chase); return; }
```

New:
```csharp
        if (dist > knightData.attackRange) { SetState(EnemyState.Chase); return; }
```

- [ ] **Step 4: Update hitbox enable calls**

Old:
```csharp
        leftHitbox.Enable(meleeData.attackDamage);
```

New:
```csharp
        leftHitbox.Enable(knightData.attackDamage);
```

Old:
```csharp
        rightHitbox.Enable(meleeData.attackDamage);
```

New:
```csharp
        rightHitbox.Enable(knightData.attackDamage);
```

- [ ] **Step 5: Update `ShouldGuard()`**

Old:
```csharp
    bool ShouldGuard() => Random.value < guardChance;
```

New:
```csharp
    bool ShouldGuard() => Random.value < knightData.guardChance;
```

### BlueKnightBrain.cs

- [ ] **Step 6: Apply the same pattern**

Old:
```csharp
    [SerializeField] private EnemyHitbox hitbox;
    [SerializeField] float guardChance = 0.2f;
    private MeleeEnemyData meleeData;
```

New:
```csharp
    [SerializeField] private EnemyHitbox hitbox;
    private KnightEnemyData knightData;
```

Update `Awake()`:
```csharp
        knightData = entity.GetData<KnightEnemyData>();
```

Update `DecideNextState()`:
```csharp
        if (dist > knightData.attackRange) { SetState(EnemyState.Chase); return; }
```

Update hitbox:
```csharp
        hitbox?.Enable(knightData.attackDamage);
```

Update guard:
```csharp
    bool ShouldGuard() => Random.value < knightData.guardChance;
```

---

## Task 6: Update Cave Brains to Use `CaveEnemyData`

**Files:**
- Modify: `Assets/_Shared_Resources/Enemies/AI/SkeletonBrain.cs`
- Modify: `Assets/_Shared_Resources/Enemies/AI/SpiderBrain.cs`
- Modify: `Assets/_Shared_Resources/Enemies/AI/SnakeBrain.cs`

**Interfaces:**
- Consumes: `CaveEnemyData.attackDamage`, `CaveEnemyData.attackRange`, `CaveEnemyData.onHitEffects`.

All three files are structurally identical. For each one:

- [ ] **Step 1: Replace `meleeData` with `caveData`**

Old:
```csharp
    [SerializeField] private EnemyHitbox hitbox;
    private MeleeEnemyData meleeData;
```

New:
```csharp
    [SerializeField] private EnemyHitbox hitbox;
    private CaveEnemyData caveData;
```

- [ ] **Step 2: Update `Awake()` data load**

Old:
```csharp
        meleeData = entity.GetData<MeleeEnemyData>();
```

New:
```csharp
        caveData = entity.GetData<CaveEnemyData>();
```

- [ ] **Step 3: Update `DecideNextState()` range check**

Old:
```csharp
        if (dist > meleeData.attackRange) { SetState(EnemyState.Chase); return; }
```

New:
```csharp
        if (dist > caveData.attackRange) { SetState(EnemyState.Chase); return; }
```

- [ ] **Step 4: Update `OnAttackHitStart()` to apply status effects**

Old:
```csharp
        hitbox?.Enable(meleeData.attackDamage);
```

New:
```csharp
        hitbox?.Enable(caveData.attackDamage, caveData.onHitEffects);
```

Repeat Steps 1-4 for `SkeletonBrain.cs`, `SpiderBrain.cs`, and `SnakeBrain.cs`.

---

## Task 7: Update Wizard Brain to Pass Throw Effects

**Files:**
- Modify: `Assets/_Shared_Resources/Enemies/AI/WizardBrain.cs`

**Interfaces:**
- Consumes: `WizardEnemyData.throwEffects`.

- [ ] **Step 1: Update `OnThrowSpawnBall()`**

Old:
```csharp
            proj.Initialize(dir, wizData.ballSpeed, wizData.throwDamage, null, entity);
```

New:
```csharp
            proj.Initialize(dir, wizData.ballSpeed, wizData.throwDamage, wizData.throwEffects, entity);
```

---

## Task 8: Create Status Effect Assets

**Files:**
- Create: `Assets/_Shared_Resources/Effects/Data/PoisonEffect.asset`
- Create: `Assets/_Shared_Resources/Effects/Data/BleedEffect.asset`
- Create: `Assets/_Shared_Resources/Effects/Data/FreezeEffect.asset`

**Interfaces:**
- Produces: `StatusEffectData` assets referenced by `CaveEnemyData` and `WizardEnemyData` assets (Task 9).

StatusEffectData script GUID (from `SlowEffect.asset`): `f2037eb76bbacee1988b62b0af18594f`.

- [ ] **Step 1: Create `PoisonEffect.asset`**

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f2037eb76bbacee1988b62b0af18594f, type: 3}
  m_Name: PoisonEffect
  m_EditorClassIdentifier: Assembly-CSharp::StatusEffectData
  kind: 0
  duration: 3
  tickRate: 1
  damagePerTick: 5
  stopsMovement: 0
  stopsAttack: 0
  speedMultiplier: 1
  knockbackForce: 0
  knockbackDuration: 0.2
  stacking: 0
```

- [ ] **Step 2: Create `BleedEffect.asset`**

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f2037eb76bbacee1988b62b0af18594f, type: 3}
  m_Name: BleedEffect
  m_EditorClassIdentifier: Assembly-CSharp::StatusEffectData
  kind: 6
  duration: 4
  tickRate: 1
  damagePerTick: 6
  stopsMovement: 0
  stopsAttack: 0
  speedMultiplier: 1
  knockbackForce: 0
  knockbackDuration: 0.2
  stacking: 0
```

- [ ] **Step 3: Create `FreezeEffect.asset`**

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f2037eb76bbacee1988b62b0af18594f, type: 3}
  m_Name: FreezeEffect
  m_EditorClassIdentifier: Assembly-CSharp::StatusEffectData
  kind: 2
  duration: 2
  tickRate: 0
  damagePerTick: 0
  stopsMovement: 1
  stopsAttack: 1
  speedMultiplier: 1
  knockbackForce: 0
  knockbackDuration: 0.2
  stacking: 0
```

- [ ] **Step 4: Create `.meta` files for the new effect assets**

`PoisonEffect.asset.meta`:
```yaml
fileFormatVersion: 2
guid: c1a70ba3c4bb4bbfa63fd88721164aef
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

`BleedEffect.asset.meta`:
```yaml
fileFormatVersion: 2
guid: d5f375049d6e4d2c8983453c10583577
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

`FreezeEffect.asset.meta`:
```yaml
fileFormatVersion: 2
guid: 7b4e3ac689b3406880e0ab3dde76dba5
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

---

## Task 9: Update Existing Data Assets to New Types and Effects

**Files:**
- Modify: `Assets/_Shared_Resources/Enemies/Data/BlackKnightData.asset`
- Modify: `Assets/_Shared_Resources/Enemies/Data/BlueKnightData.asset`
- Modify: `Assets/_Shared_Resources/Enemies/Data/SkeletonData.asset`
- Modify: `Assets/_Shared_Resources/Enemies/Data/SpiderData.asset`
- Modify: `Assets/_Shared_Resources/Enemies/Data/SnakeData.asset`
- Modify: `Assets/_Shared_Resources/Enemies/Data/WizardData.asset`

**Interfaces:**
- Consumes: GUIDs from `KnightEnemyData.cs.meta`, `CaveEnemyData.cs.meta`, and status-effect `.meta` files.

Existing `MeleeEnemyData` script GUID: `12ffe883e88ca596786057a41c23419c`.
`KnightEnemyData` script GUID: `23612c8a809e496e8566ac285d01eaa4`.
`CaveEnemyData` script GUID: `bee7f887eb1948eea21972ed80405273`.
`SlowEffect` asset GUID: `6ed46aef80dfdf01287c162141ef10ae`.
`PoisonEffect` asset GUID: `c1a70ba3c4bb4bbfa63fd88721164aef`.
`BleedEffect` asset GUID: `d5f375049d6e4d2c8983453c10583577`.
`FreezeEffect` asset GUID: `7b4e3ac689b3406880e0ab3dde76dba5`.

- [ ] **Step 1: Rewrite `BlackKnightData.asset`**

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 23612c8a809e496e8566ac285d01eaa4, type: 3}
  m_Name: BlackKnightData
  m_EditorClassIdentifier: Assembly-CSharp::KnightEnemyData
  maxHealth: 150
  moveSpeed: 3
  spawnVFXPrefab: {fileID: 7154831219701261189, guid: 214d884724b865c1eab63ad02347873a, type: 3}
  deathVFXPrefab: {fileID: 7154831219701261189, guid: 7698cce7c2fa4d2e694b2dec1e4c3095, type: 3}
  spawnFadeDuration: 0.5
  deathFadeDuration: 0.5
  attackDamage: 20
  attackRange: 1.2
  attackCooldown: 0.75
  guardChance: 0.3
```

- [ ] **Step 2: Rewrite `BlueKnightData.asset`**

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 23612c8a809e496e8566ac285d01eaa4, type: 3}
  m_Name: BlueKnightData
  m_EditorClassIdentifier: Assembly-CSharp::KnightEnemyData
  maxHealth: 150
  moveSpeed: 3
  spawnVFXPrefab: {fileID: 7154831219701261189, guid: 214d884724b865c1eab63ad02347873a, type: 3}
  deathVFXPrefab: {fileID: 7154831219701261189, guid: 7698cce7c2fa4d2e694b2dec1e4c3095, type: 3}
  spawnFadeDuration: 0.5
  deathFadeDuration: 0.5
  attackDamage: 20
  attackRange: 1.2
  attackCooldown: 1.5
  guardChance: 0.2
```

- [ ] **Step 3: Rewrite `SkeletonData.asset`**

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: bee7f887eb1948eea21972ed80405273, type: 3}
  m_Name: SkeletonData
  m_EditorClassIdentifier: Assembly-CSharp::CaveEnemyData
  maxHealth: 150
  moveSpeed: 4
  spawnVFXPrefab: {fileID: 7154831219701261189, guid: 214d884724b865c1eab63ad02347873a, type: 3}
  deathVFXPrefab: {fileID: 7154831219701261189, guid: 7698cce7c2fa4d2e694b2dec1e4c3095, type: 3}
  spawnFadeDuration: 0.5
  deathFadeDuration: 0.5
  attackDamage: 20
  attackRange: 1
  attackCooldown: 0.75
  onHitEffects:
  - {fileID: 11400000, guid: d5f375049d6e4d2c8983453c10583577, type: 2}
```

- [ ] **Step 4: Rewrite `SpiderData.asset`**

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: bee7f887eb1948eea21972ed80405273, type: 3}
  m_Name: SpiderData
  m_EditorClassIdentifier: Assembly-CSharp::CaveEnemyData
  maxHealth: 150
  moveSpeed: 3
  spawnVFXPrefab: {fileID: 7154831219701261189, guid: 214d884724b865c1eab63ad02347873a, type: 3}
  deathVFXPrefab: {fileID: 7154831219701261189, guid: 7698cce7c2fa4d2e694b2dec1e4c3095, type: 3}
  spawnFadeDuration: 0.5
  deathFadeDuration: 0.5
  attackDamage: 20
  attackRange: 1.2
  attackCooldown: 1
  onHitEffects:
  - {fileID: 11400000, guid: 6ed46aef80dfdf01287c162141ef10ae, type: 2}
  - {fileID: 11400000, guid: c1a70ba3c4bb4bbfa63fd88721164aef, type: 2}
```

- [ ] **Step 5: Rewrite `SnakeData.asset`**

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: bee7f887eb1948eea21972ed80405273, type: 3}
  m_Name: SnakeData
  m_EditorClassIdentifier: Assembly-CSharp::CaveEnemyData
  maxHealth: 150
  moveSpeed: 3
  spawnVFXPrefab: {fileID: 7154831219701261189, guid: 214d884724b865c1eab63ad02347873a, type: 3}
  deathVFXPrefab: {fileID: 7154831219701261189, guid: 7698cce7c2fa4d2e694b2dec1e4c3095, type: 3}
  spawnFadeDuration: 0.5
  deathFadeDuration: 0.5
  attackDamage: 20
  attackRange: 1.2
  attackCooldown: 1
  onHitEffects:
  - {fileID: 11400000, guid: c1a70ba3c4bb4bbfa63fd88721164aef, type: 2}
```

- [ ] **Step 6: Update `WizardData.asset`**

Keep the existing YAML and append `throwEffects` after `pigPrefab`:

```yaml
  pigPrefab: {fileID: 8651997999754049487, guid: 1c670ab539dae78c69978801aa376281, type: 3}
  throwEffects:
  - {fileID: 11400000, guid: 7b4e3ac689b3406880e0ab3dde76dba5, type: 2}
```

---

## Task 10: Verify Prefabs and Compilation

**Files:**
- Verify: `Assets/_Shared_Resources/Enemies/BlackKnight/BlackKnight.prefab`
- Verify: `Assets/_Shared_Resources/Enemies/BlueKnight/BlueKnight.prefab`
- Verify: `Assets/_Shared_Resources/Enemies/Skeleton/Skeleton.prefab`
- Verify: `Assets/_Shared_Resources/Enemies/Spider/Spider.prefab`
- Verify: `Assets/_Shared_Resources/Enemies/Snake/Snake.prefab`

- [ ] **Step 1: Confirm prefab data references did not change**

Each prefab's `EnemyEntity.data` field should still reference the same asset GUID as before (the asset's own GUID is unchanged). No prefab YAML edits are required.

- [ ] **Step 2: Trigger a Unity script compile**

Open the project in Unity or run a compile check. Expected result: zero compile errors.

- [ ] **Step 3: Spot-check data assets in Inspector**

Open `BlackKnightData`, `BlueKnightData`, `SkeletonData`, `SpiderData`, `SnakeData`, and `WizardData` in the Unity Inspector. Confirm:
- Knights show `Guard Chance`.
- Skeleton/Spider/Snake show `On Hit Effects` with the correct assets.
- Wizard shows `Throw Effects` with `FreezeEffect`.

- [ ] **Step 4: Optional Play Mode smoke test**

Enter Play Mode, spawn each affected enemy, and verify:
- Black Knight and Blue Knight still enter guard stance.
- Snake applies poison, Spider applies slow+poison, Skeleton applies bleed.
- Wizard explosion ball applies freeze.

