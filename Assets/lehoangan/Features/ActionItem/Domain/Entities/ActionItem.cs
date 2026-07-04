using UnityEngine;

[CreateAssetMenu(fileName = "ActionItem", menuName = "Scriptable Objects/ActionItem")]
public class ActionItem : ScriptableObject
{
    public string itemName;
    public string itemDescription;
    public Sprite icon;
    public int price = 100;
}
