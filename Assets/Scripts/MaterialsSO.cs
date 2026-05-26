using UnityEngine;

public enum MaterialType { Wood, Fish }

[CreateAssetMenu(fileName = "New Material", menuName = "Materials/Element")]
public class MaterialsSO : ScriptableObject
{
    public MaterialType type;
    public Sprite icon;
    public GameObject prefab;
    public GameObject vfx;
}
