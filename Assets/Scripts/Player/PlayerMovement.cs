﻿using System.Collections;
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
    public float tapSpeed = 1f; //in seconds
    private float lastTapTime = 0;

    Vector3 velocity;
    bool isGrounded;
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
        if (!isSprinting && (Time.time - lastTapTime) < tapSpeed)
        {
            isSprinting = true;
        }
        // If user wants to move forward
        if (Input.GetKeyDown(KeyCode.W))
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
                // Set speed to walking speed
                speed = walkSpeed;
                // Play walking sound
                if (!walkingSound.isPlaying)
                {
                    walkingSound.Play();
                }
            }
            lastTapTime = Time.time;
        }
        // If not moving anymore because not pressing w
        if (Input.GetKeyUp(KeyCode.W))
        {
            // Turn off sounds
            walkingSound.Stop();
            sprintingSound.Stop();
            // Turn off sprinting and set speed to 0 since player is not moving
            isSprinting = false;
            speed = 0;
        }
        // Move
        Vector3 move = transform.right * x + transform.forward * z;
        Debug.Log(speed);
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