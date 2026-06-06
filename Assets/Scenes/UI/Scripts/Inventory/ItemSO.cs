using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "New Item")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public int valorCura = 20;
    public bool ehConsumivel = false;
    public Sprite icon;
    public int maxStackSize;
    public GameObject itemPrefab;
    public GameObject handItemPrefab;
}
