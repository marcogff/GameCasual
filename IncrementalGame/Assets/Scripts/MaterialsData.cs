using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MaterialsData : MonoBehaviour
{
    public int currentMaterialBuild;
    public int maxMaterialsBuild;
    public GameObject obj;
    public MaterialsSO materialData;
    public Transform spawnPoint;
    public List<GameObject> elementsInBuild = new List<GameObject>();
    public GameObject prefabLand;
    public GameObject prefabLandNext;
    bool _isDeploying;
    private bool _currentDeployed;
    public BoxCollider limit;
    public TextMeshProUGUI currentText;
    public TextMeshProUGUI needText;
    public GameObject parentText;
    public bool dropItems;
    public bool canDrop = true;
    private bool coroutineExecuted;
    public int currentElements = 10;
    // private bool executed = false;

    void Start()
    {
        LeanTween.moveLocalX(this.transform.GetChild(0).GetChild(0).GetChild(1).gameObject, 2.5f, 1f).setLoopPingPong();

        if (dropItems)
        {
            LeanTween.scale(parentText, new Vector3(1.2f, 1.2f, 1.2f), .3f).setLoopPingPong();
        }
    }

    void Update()
    {
        if (dropItems)
        {
            if (!canDrop)
            {
                // LeanTween.scale(obj, Vector3.zero, .3f).setEaseInCirc();
                ExecuteEffect();

                this.transform.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false);
            }
            else
            {
                // LeanTween.scale(obj, Vector3.one, .3f).setEaseInCirc();
                this.transform.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true);

            }
        }

        if (currentText != null || needText != null)
        {
            currentText.text = elementsInBuild.Count.ToString();
            needText.text = maxMaterialsBuild.ToString();
        }


        if (currentElements == 10)
        {
            if (dropItems)
            {
                if (coroutineExecuted)
                {
                    return;
                }
                else
                {
                    StartCoroutine(Fill(7));
                }
            }
        }

        if (prefabLand != null)
        {
            if (elementsInBuild.Count == maxMaterialsBuild)
            {
                if (parentText != null)
                {
                    LeanTween.scale(parentText, Vector3.zero, .2f);
                    Destroy(this.transform.GetChild(0).gameObject);
                    Destroy(parentText);
                }

                if (_currentDeployed)
                {

                    for (int i = 0; i < elementsInBuild.Count; i++)

                    {
                        Destroy(elementsInBuild[i]);
                    }
                    return;
                }

                _isDeploying = true;
                DeployLand();
            }

        }

    }

    void DeployLand()
    {
        if (_isDeploying)
        {
            // prefabLandNext.SetActive(true);
            limit.enabled = false;
            LeanTween.scale(prefabLand, Vector3.one, .3f).setEaseInCirc();
            _isDeploying = false;
            _currentDeployed = true;
        }
    }

    void ExecuteEffect()
    {
        bool executed = false;

        if (!executed)
        {
            executed = true;
            
        }

    }

    private IEnumerator Fill(int time)
    {

        coroutineExecuted = true;
        LeanTween.scale(obj, Vector3.zero, .3f);
        canDrop = false;

        currentElements = 10;

        yield return new WaitForSeconds(time);

        LeanTween.scale(obj, Vector3.one, .3f);

        canDrop = true;
        coroutineExecuted = false;
        // executed = false;
        currentElements = 0;

    }
}
