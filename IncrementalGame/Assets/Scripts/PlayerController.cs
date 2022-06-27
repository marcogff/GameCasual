using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float horizontalMove;
    private float verticalMove;
    private CharacterController player;
    // private Vector3 playerMovement;
    [SerializeField] private float playerSpeed;
    void Start()
    {
        player = this.GetComponent<CharacterController>();
        // playerMovement = new Vector3(0, 0, 0 );
    }

    void Update()
    {
        horizontalMove = Input.GetAxis("Horizontal");
        verticalMove = Input.GetAxis("Vertical");
        
    }

    void FixedUpdate()
    {
        player.Move(new Vector3(-horizontalMove, 0, -verticalMove) * playerSpeed * Time.deltaTime);
    }
}
