
using UnityEngine;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(PlayerControls))]
public class FirstPersonMovement : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float mouseSensitivity = 2f;
    public Transform playerCamera;

    private CharacterController controller;
    private PlayerControls controls;
    private float verticalRotation = 0f;
    private float verticalVelocity = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        controls = GetComponent<PlayerControls>();
    }

    void Update()
    {
        bool controlsActive = controls.GameplayActive;
        // Mouse look
        float sensitivity = controls.UsesTouch ? 1f : mouseSensitivity;
        float mouseX = controlsActive ? controls.LookDelta.x * sensitivity : 0f;
        float mouseY = controlsActive ? controls.LookDelta.y * sensitivity : 0f;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        playerCamera.localRotation =
            Quaternion.Euler(verticalRotation, 0f, 0f);

        // WASD movement
        float x = controlsActive ? controls.Movement.x : 0f;
        float z = controlsActive ? controls.Movement.y : 0f;

        Vector3 move =
            transform.right * x + transform.forward * z;

        move = Vector3.ClampMagnitude(move, 1f);

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 velocity =
            move * moveSpeed + Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

    }
}
