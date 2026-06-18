using UnityEngine;

public enum MaterialType { Wood, Fish }

/// <summary>
/// ScriptableObject that defines a collectable resource type.
/// Create via: right-click in Project → Materials → Element
/// </summary>
[CreateAssetMenu(fileName = "New Material", menuName = "Materials/Element")]
public class MaterialsSO : ScriptableObject
{
    [Header("Identity")]
    public MaterialType type;
    public string       displayName;    // shown in UI — e.g. "Wood", "Fish"
    public Color        resourceColor = Color.white; // accent colour for HUD / counters

    [Header("Assets")]
    public Sprite     icon;
    public GameObject prefab;
    public GameObject vfx;
}
