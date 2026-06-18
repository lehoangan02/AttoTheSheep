using UnityEngine;

[System.Serializable]
public class EffectData
{
    public Effect effect;           // reference to Effect SO asset
    public float duration = 3f;     // how long the effect lasts (0 = use effect's default)
    public int damagePerTick;       // override damage per tick (0 = use effect's default)
}
