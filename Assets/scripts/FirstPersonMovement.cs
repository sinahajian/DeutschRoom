
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
    public enum BodyPosture { Standing, Sitting, Lying }
    public BodyPosture Posture { get; private set; }
    private float standingHeight, standingEye, feetOffset;
    private Vector3 standingCenter;
    public Vector3 InitialPosition { get; private set; }
    public Quaternion InitialRotation { get; private set; }
    public struct PracticeView
    {
        public Vector3 position, cameraPosition, center;
        public Quaternion rotation;
        public float pitch, height;
        public BodyPosture posture;
    }
    public PracticeView CapturePracticeView() => new PracticeView {
        position = transform.position, rotation = transform.rotation, pitch = verticalRotation,
        cameraPosition = playerCamera.localPosition, height = controller.height, center = controller.center, posture = Posture
    };
    public void RestorePracticeView(PracticeView view)
    {
        bool active = controller.enabled; controller.enabled = false;
        transform.SetPositionAndRotation(view.position, view.rotation);
        verticalRotation = view.pitch; verticalVelocity = 0; Posture = view.posture;
        controller.height = view.height; controller.center = view.center;
        playerCamera.localPosition = view.cameraPosition;
        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
        controller.enabled = active; Physics.SyncTransforms();
    }
    public void StartPracticeView()
    {
        RestorePracticeView(new PracticeView { position = InitialPosition, rotation = InitialRotation,
            pitch = 0, posture = BodyPosture.Standing, height = standingHeight, center = standingCenter,
            cameraPosition = new Vector3(playerCamera.localPosition.x, standingEye, playerCamera.localPosition.z) });
    }

    public bool ChangePosture(int direction)
    {
        if (controller == null || playerCamera == null) return false;
        int next = Mathf.Clamp((int)Posture + direction, 0, 2);
        float height = HeightFor((BodyPosture)next);
        if (height > controller.height && !HasHeadroom(height)) return false;
        Posture = (BodyPosture)next;
        return true;
    }

    private float HeightFor(BodyPosture posture) => posture == BodyPosture.Standing ? standingHeight :
        posture == BodyPosture.Sitting ? Mathf.Max(controller.radius * 2, 1.05f) : controller.radius * 2 + .02f;

    private bool HasHeadroom(float height)
    {
        float radius = Mathf.Max(.05f, controller.radius - .025f);
        Vector3 bottom = transform.TransformPoint(new Vector3(standingCenter.x, feetOffset + radius + .03f, standingCenter.z));
        Vector3 top = transform.TransformPoint(new Vector3(standingCenter.x, feetOffset + height - radius, standingCenter.z));
        foreach (Collider hit in Physics.OverlapCapsule(bottom, top, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(transform)) return false;
        return true;
    }

    private void UpdatePosture()
    {
        float target = HeightFor(Posture);
        // Recheck while rising in case the player moves under an obstacle mid-transition.
        if (target > controller.height && !HasHeadroom(target)) target = controller.height;
        controller.height = Mathf.MoveTowards(controller.height, target, Time.deltaTime * 3f);
        controller.center = new Vector3(standingCenter.x, feetOffset + controller.height / 2f, standingCenter.z);
        controller.stepOffset = Mathf.Min(.3f, controller.height * .25f);
        float eye = Mathf.Lerp(.42f, standingEye, Mathf.InverseLerp(HeightFor(BodyPosture.Lying), standingHeight, controller.height));
        Vector3 cameraPosition = playerCamera.localPosition;
        cameraPosition.y = eye;
        playerCamera.localPosition = cameraPosition;
    }

    void Start()
    {
        InitialPosition = transform.position; InitialRotation = transform.rotation;
        controller = GetComponent<CharacterController>();
        controls = GetComponent<PlayerControls>();
        standingHeight = controller.height;
        standingCenter = controller.center;
        feetOffset = standingCenter.y - standingHeight / 2f;
        standingEye = playerCamera.localPosition.y;
        if (GetComponent<FirstPersonBody>() == null) gameObject.AddComponent<FirstPersonBody>();
    }

    void Update()
    {
        if (GetComponent<RoomExperience>()?.MenuOpen == true) return;
        bool controlsActive = controls.GameplayActive;
        if (controlsActive && controls.LowerPosturePressed) ChangePosture(1);
        if (controlsActive && controls.RaisePosturePressed) ChangePosture(-1);
        UpdatePosture();
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
            move * moveSpeed * (Posture == BodyPosture.Standing ? 1f : Posture == BodyPosture.Sitting ? .5f : .25f) + Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

    }
}
