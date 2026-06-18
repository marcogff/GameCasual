using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-spawning objective HUD: shows how many build sites are complete
/// ("Village  1 / 3") at the top-center, and pops when the count goes up.
/// Reads MaterialsData.IsBuildSite / IsCompleted. Hidden if the scene has no
/// build sites. No scene/prefab wiring.
/// </summary>
public class ObjectiveTracker : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindFirstObjectByType<ObjectiveTracker>() != null) return;
        new GameObject("ObjectiveTracker").AddComponent<ObjectiveTracker>();
    }

    private MaterialsData[] _sites;
    private TextMeshProUGUI _text;
    private GameObject _root;
    private int _lastDone = -1;
    private float _timer;
    private const float CheckInterval = 0.4f;

    void Start()
    {
        BuildUI();
        StartCoroutine(FindSitesNextFrame());
    }

    IEnumerator FindSitesNextFrame()
    {
        yield return null; // let MaterialsData.Start run
        var all = FindObjectsByType<MaterialsData>(FindObjectsSortMode.None);
        var list = new List<MaterialsData>();
        foreach (var m in all)
            if (m != null && m.IsBuildSite) list.Add(m);
        _sites = list.ToArray();

        _root.SetActive(_sites.Length > 0);
    }

    void Update()
    {
        if (_sites == null || _sites.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer < CheckInterval) return;
        _timer = 0f;

        int done = 0;
        foreach (var s in _sites)
            if (s != null && s.IsCompleted) done++;

        if (done == _lastDone) return;
        _lastDone = done;

        _text.text = $"VILLAGE   {done} / {_sites.Length}";
        Pop();
    }

    void Pop()
    {
        LeanTween.cancel(_text.gameObject);
        _text.transform.localScale = Vector3.one;
        LeanTween.scale(_text.gameObject, Vector3.one * 1.25f, 0.12f).setEaseOutBack()
            .setOnComplete(() => LeanTween.scale(_text.gameObject, Vector3.one, 0.12f));
    }

    void BuildUI()
    {
        _root = new GameObject("ObjectiveCanvas");
        _root.transform.SetParent(transform, false);
        var canvas        = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 85;
        _root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        var pill = new GameObject("Pill", typeof(RectTransform));
        pill.transform.SetParent(_root.transform, false);
        var prt = pill.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 1f);
        prt.anchoredPosition = new Vector2(0f, -20f);
        prt.sizeDelta = new Vector2(320f, 60f);
        var img = pill.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.55f);

        _text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        _text.transform.SetParent(pill.transform, false);
        var trt = _text.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        _text.text      = "VILLAGE   0 / 0";
        _text.fontSize  = 26f;
        _text.color     = Color.white;
        _text.fontStyle = FontStyles.Bold;
        _text.alignment = TextAlignmentOptions.Center;

        _root.SetActive(false);
    }
}
