using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialsData : MonoBehaviour
{
    public int currentMaterialBuild;
    public int maxMaterialsBuild;
    public MaterialsSO materialData;
    public Transform spawnPoint;
    public List<GameObject> elementsInBuild = new List<GameObject>();

    void Update()
    {
        if (elementsInBuild.Count == maxMaterialsBuild)
        {
            // elementsInBuild.Clear();
        }
    }
}
