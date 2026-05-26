using UnityEngine;

public class MoveBagPos : MonoBehaviour
{
    [SerializeField] private Vector3[] _randompositions;

    private int _lastIndex = -1;

    void Update()
    {
        int index = GameManager.Instance.playerController.bagPosIndex;

        // was hardcoded to 5 — use array length so adding positions "just works"
        if (index >= _randompositions.Length)
        {
            index = 0;
            GameManager.Instance.playerController.bagPosIndex = 0;
        }

        // only write transform when the index actually changed
        if (index != _lastIndex)
        {
            transform.localPosition = _randompositions[index];
            _lastIndex = index;
        }
    }
}
