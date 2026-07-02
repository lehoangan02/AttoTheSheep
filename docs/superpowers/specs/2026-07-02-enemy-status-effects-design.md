# Enemy Status Effects & Family Data Design

## Summary
Add status effects to Skeleton, Spider, Snake, and Wizard attacks by introducing two new enemy data classes (`KnightEnemyData`, `CaveEnemyData`) and extending `WizardEnemyData`. Move guard configuration out of knight brain scripts and into data assets. Create the missing status-effect ScriptableObject assets and wire them into the corresponding enemy prefabs/data assets.

## Context
The project already has a unified status-effect system (`StatusEffectController`, `StatusEffectData`, `EffectKind`). Only the Troll is currently wired to use status effects (`TrollEnemyData.smashEffects` / `chargeEffects`), but its arrays are empty. The existing `MeleeEnemyData` only carries base melee stats (`attackDamage`, `attackRange`, `attackCooldown`) and does not support status effects or guard behavior.

## Goals
1. Create `KnightEnemyData` that extends `MeleeEnemyData` and owns `guardChance`.
2. Create `CaveEnemyData` that extends `MeleeEnemyData` and owns `StatusEffectData[] onHitEffects`.
3. Extend `WizardEnemyData` with `StatusEffectData[] throwEffects`.
4. Create new status-effect assets: `PoisonEffect`, `BleedEffect`, `FreezeEffect`. Reuse existing `SlowEffect` for Spider.
5. Update knight brains to read guard chance from data instead of hard-coded fields.
6. Update Skeleton/Spider/Snake brains to apply their data-driven status effects on melee hit.
7. Update Wizard brain to pass freeze effect to its explosion-ball projectile.
8. Convert existing data assets and prefabs to the new types and assignments.

## Non-Goals
- Do not add status effects to Troll attacks (leave arrays empty).
- Do not change the base `EnemyData` or `MeleeEnemyData` classes.
- Do not change non-combat enemy behavior (movement, targeting, death).

## Design Details

### 1. New Data Classes

#### `KnightEnemyData.cs`
```csharp
[CreateAssetMenu(fileName = "KnightEnemyData", menuName = "Gameplay/Enemies/Knight Enemy Data")]
public class KnightEnemyData : MeleeEnemyData
{
    [Header("Guard")]
    [Range(0f, 1f)]
    public float guardChance = 0.2f;
}
```

#### `CaveEnemyData.cs`
```csharp
[CreateAssetMenu(fileName = "CaveEnemyData", menuName = "Gameplay/Enemies/Cave Enemy Data")]
public class CaveEnemyData : MeleeEnemyData
{
    [Header("Status Effects")]
    [Tooltip("Status effects applied on a successful melee hit.")]
    public StatusEffectData[] onHitEffects;
}
```

### 2. Extend `WizardEnemyData.cs`
Add to the existing class:
```csharp
[Header("Throw Status Effects")]
public StatusEffectData[] throwEffects;
```

### 3. Status Effect Kind Extension
Add `Bleed` to the `EffectKind` enum in `StatusEffectData.cs`:

```csharp
public enum EffectKind { Poison, Burn, Freeze, Stun, Knockback, Slow, Bleed }
```

`StatusEffectController.HasEffect` only has client-side approximations for `Freeze`, `Stun`, and `Slow`; adding `Bleed` is safe and does not require extra controller logic.

### 4. Status Effect Assets
Create under `Assets/_Shared_Resources/Effects/Data/`:

| Asset | EffectKind | Purpose |
|-------|-----------|---------|
| `PoisonEffect.asset` | `Poison` | Snake melee, Spider melee |
| `BleedEffect.asset` | `Bleed` | Skeleton melee |
| `FreezeEffect.asset` | `Freeze` | Wizard explosion ball |
| `SlowEffect.asset` | `Slow` | Spider melee (existing asset) |

### 5. Brain Script Updates

#### `BlackKnightBrain.cs` & `BlueKnightBrain.cs`
- Remove `[SerializeField] float guardChance = ...;`.
- Add `private KnightEnemyData knightData;` and load it in `Awake()`.
- Change `ShouldGuard()` to use `knightData.guardChance`.

#### `SkeletonBrain.cs`, `SpiderBrain.cs`, `SnakeBrain.cs`
- Replace `MeleeEnemyData meleeData` with `CaveEnemyData caveData`.
- In `OnAttackHitStart()`, pass `caveData.onHitEffects` to `hitbox.Enable(...)`:
  ```csharp
  hitbox.Enable(caveData.attackDamage, caveData.onHitEffects);
  ```

#### `WizardBrain.cs`
- In `OnThrowSpawnBall()`, change the projectile initialization to pass `wizData.throwEffects`:
  ```csharp
  proj.Initialize(dir, wizData.ballSpeed, wizData.throwDamage, wizData.throwEffects, entity);
  ```

### 6. Asset & Prefab Updates

#### Data Assets
| Asset | Current Type | New Type | New Fields / Values |
|-------|-------------|----------|---------------------|
| `BlackKnightData.asset` | `MeleeEnemyData` | `KnightEnemyData` | `guardChance: 0.3` |
| `BlueKnightData.asset` | `MeleeEnemyData` | `KnightEnemyData` | `guardChance: 0.2` |
| `SkeletonData.asset` | `MeleeEnemyData` | `CaveEnemyData` | `onHitEffects: [BleedEffect]` |
| `SpiderData.asset` | `MeleeEnemyData` | `CaveEnemyData` | `onHitEffects: [SlowEffect, PoisonEffect]` |
| `SnakeData.asset` | `MeleeEnemyData` | `CaveEnemyData` | `onHitEffects: [PoisonEffect]` |
| `WizardData.asset` | `WizardEnemyData` | `WizardEnemyData` | `throwEffects: [FreezeEffect]` |

#### Prefabs
- The prefabs already reference their data assets by GUID, and the asset file GUIDs do not change. Therefore the prefab `data` references do not need to be modified.
- The change happens in each **data asset** YAML: update its `m_Script` GUID to point to the new class (`KnightEnemyData` or `CaveEnemyData`) and add the new serialized fields (`guardChance` or `onHitEffects`).
- Verify each prefab still loads the correct asset type after the asset YAML is updated.

## Risks & Mitigations
| Risk | Mitigation |
|------|-----------|
| Changing `m_Script` GUID in prefab/data YAML manually could corrupt references. | Keep the target asset fileID unchanged; only change the `m_Script` GUID and add/remove fields. Verify each prefab still compiles. |
| `Bleed` is not a real `EffectKind`. | Extend the enum with `Bleed`; the status controller handles it generically. |
| Existing serialized `MeleeEnemyData` fields are preserved in new subclasses. | Both new classes inherit from `MeleeEnemyData`, so `attackDamage`, `attackRange`, and `attackCooldown` remain valid. |

## Testing
- Open each affected prefab and confirm the data asset reference is valid.
- Enter Play Mode and verify: knights still guard, snake poisons, spider slows+poisons, skeleton bleeds, wizard freezes.
- Confirm Troll status-effect arrays remain empty.
