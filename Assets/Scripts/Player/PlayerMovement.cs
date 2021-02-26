using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public AudioSource walkingSound;
    public AudioSource sprintingSound;

    [Header("Info")]
    public float walkSpeed = 10f;
    public float sprintSpeed = 30f;
    public float gravity = -40f;
    public float jumpHeight = 5f;

    public float speed = 0;
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    public float tapSpeed = 0.2f; //in seconds
    private float lastTapTime = 0;

    Vector3 velocity;
    bool isGrounded;
    // Used to detect if the user has made their first move
    bool firstMoveComplete = false;
    bool isSprinting;

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Check if grounded
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Sprinting
        // If the user has not made their first move then we do not care about elapsed time for
        // the double tap, because they have yet to press the "w" key
        if (Input.GetKeyDown(KeyCode.W) && firstMoveComplete && (Time.time - lastTapTime) < tapSpeed)
        {
            isSprinting = true;
        }
        // If user wants to move
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.A))
        {
            // If sprinting
            if (isSprinting)
            {
                // Set speed to sprinting speed
                speed = sprintSpeed;
                // Play sprinting sound
                if (!sprintingSound.isPlaying)
                {
                    sprintingSound.Play();
                }
            }
            // Otherwise walking
            else
            {
                // Detect elapsed time for only w since only sprint forward for now
                if (Input.GetKeyDown(KeyCode.W))
                {
                    lastTapTime = Time.time;
                    firstMoveComplete = true;
                }
                // Set speed to walking speed
                speed = walkSpeed;
                // Play walking sound
                if (!walkingSound.isPlaying)
                {
                    walkingSound.Play();
                }
            }
        }
        // If not moving anymore
        else if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.A))
        {
            // If player is sprinting and the stop pressing w, stop spritning
            if (isSprinting && Input.GetKeyUp(KeyCode.W))
            {
                sprintingSound.Stop();
                isSprinting = false;
            }
            walkingSound.Stop();
        }

        // Move
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        // Jumping
        if (Input.GetButton("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // No vertical movment
        if (Input.GetKey(KeyCode.C))
        {
            velocity.y = z;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

    }
}