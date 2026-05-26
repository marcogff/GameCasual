using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MaterialsData : MonoBehaviour
{
    public int maxMaterialsBuild;
    public GameObject obj;
    public MaterialsSO materialData;
    public Transform spawnPoint;
    public List<GameObject> elementsInBuild = new List<GameObject>();
    public GameObject prefabLand;
    public BoxCollider limit;
    public TextMeshProUGUI currentText;
    public TextMeshProUGUI needText;
    public GameObject parentText;
    public bool dropItems;
    public bool canDrop = true;
    public int currentElements = 10;

    private bool _currentDeployed;
    private bool _coroutineExecuted;
    private GameObject _indicatorObject;

    void Start()
    {
        if (dropItems)
        {
            LeanTween.scale(parentText, new Vector3(1.2f, 1.2f, 1.2f), .3f).setLoopPingPong();
            _indicatorObject = transform.GetChild(0).GetChild(0).GetChild(1).gameObject;
        }
    }

    void Update()
    {
        if (dropItems && _indicatorObject != null)
            _indicatorObject.SetActive(canDrop);

        // was || (OR): would NPE if only one text was assigned; both must be non-null
        if (currentText != null && needText != null)
        {
            currentText.text = elementsInBuild.Count.ToString();
            needText.text = maxMaterialsBuild.ToString();
        }

        if (currentElements == 10 && dropItems && !_coroutineExecuted)
            StartCoroutine(Fill(7));

        if (prefabLand != null && elementsInBuild.Count == maxMaterialsBuild)
        {
            if (parentText != null)
            {
                LeanTween.scale(parentText, Vector3.zero, .2f);
                Destroy(transform.GetChild(0).gameObject);
                Destroy(parentText);
                parentText = null; // prevent re-entry next frame before Unity finalizes Destroy
            }

            if (_currentDeployed)
            {
                for (int i = 0; i < elementsInBuild.Count; i++)
                    Destroy(elementsInBuild[i]);
                elementsInBuild.Clear(); // stop re-processing next frame
                return;
            }

            DeployLand();
        }
    }

    void DeployLand()
    {
        // _isDeploying guard was redundant — this is only reached when !_currentDeployed
        limit.enabled = false;
        LeanTween.scale(prefabLand, Vector3.one, .3f).setEaseInCirc();
        _currentDeployed = true;
    }

    private IEnumerator Fill(int time)
    {
        _coroutineExecuted = true;
        LeanTween.scale(obj, Vector3.zero, .3f);
        canDrop = false;
        currentElements = 10;

        yield return new WaitForSeconds(time);

        LeanTween.scale(obj, Vector3.one, .3f);
        canDrop = true;
        _coroutineExecuted = false;
        currentElements = 0;
    }
}
