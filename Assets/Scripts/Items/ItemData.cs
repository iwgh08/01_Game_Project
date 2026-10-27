using UnityEngine;

[CreateAssetMenu(menuName = "방과후 방송/아이템", fileName = "Item")]
public class ItemData : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
}
