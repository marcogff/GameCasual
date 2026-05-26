using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveBagPos : MonoBehaviour
{   
    [SerializeField]
    private Vector3[] _randompositions;
    
    void Update()
    {
        if (GameManager.Instance.playerController.bagPosIndex >= 5)
        {
            GameManager.Instance.playerController.bagPosIndex = 0;
        }

        this.gameObject.transform.localPosition = _randompositions[GameManager.Instance.playerController.bagPosIndex];
    }
}
