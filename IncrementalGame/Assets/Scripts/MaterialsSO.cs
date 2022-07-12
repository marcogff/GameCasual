using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Material", menuName = "Materials/Element")]
public class MaterialsSO : ScriptableObject
{
    public string type;
    public Sprite icon;
    public GameObject prefab;
    public GameObject vfx;

}
