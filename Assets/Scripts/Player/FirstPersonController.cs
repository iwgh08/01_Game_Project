using UnityEngine;
using UnityEngine.InputSystem;

// CharacterController 기반 1인칭 이동. 입력은 프로젝트 전역 액션(InputSystem_Actions)의 Player 맵을 쓴다.
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] float walkSpeed = 2.0f;
    [SerializeField] float sprintSpeed = 3.8f;
    [SerializeField] float crouchSpeed = 1.1f;
    [SerializeField] float acceleration = 14f;
    [SerializeField] float gravity = -20f;

    [Header("시점")]
    [SerializeField] Transform cameraRoot;
    [SerializeField] float lookScale = 0.08f;
    [SerializeField] float maxPitch = 80f;

    [Header("앉기")]
    [SerializeField] float standHeight = 1.75f;
    [SerializeField] float crouchHeight = 1.1f;
    [SerializeField] float standEyeHeight = 1.6f;
    [SerializeField] float crouchEyeHeight = 0.95f;
    [SerializeField] float crouchTransitionSpeed = 5f;

    [Header("걸음 흔들림")]
    [SerializeField] float bobStride = 1.5f;     // 흔들림 한 주기(두 걸음)에 해당하는 이동 거리
    [SerializeField] float bobAmplitude = 0.03f;

    CharacterController controller;
    InputAction moveAction, lookAction, sprintAction, crouchAction;
    Vector3 planarVelocity;
    float verticalVelocity, pitch, eyeHeight, bobDistance, bobWeight;

    public bool IsCrouching { get; private set; }
    public bool IsGrounded => controller.isGrounded;
    public float HorizontalSpeed { get; private set; }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        moveAction = InputSystem.actions.FindAction("Player/Move", true);
        lookAction = InputSystem.actions.FindAction("Player/Look", true);
        sprintAction = InputSystem.actions.FindAction("Player/Sprint", true);
        crouchAction = InputSystem.actions.FindAction("Player/Crouch", true);
        eyeHeight = standEyeHeight;
    }

    void Update()
    {
        bool canControl = GameManager.Instance == null || GameManager.Instance.IsPlaying;
        Vector2 input = canControl ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        if (canControl)
        {
            Look();
            if (crouchAction.WasPressedThisFrame()) ToggleCrouch();
        }
        Move(input);
        UpdateHeightAndBob();
    }

    void Look()
    {
        Vector2 delta = lookAction.ReadValue<Vector2>() * (lookScale * GameSettings.MouseSensitivity);
        transform.Rotate(0f, delta.x, 0f);
        pitch = Mathf.Clamp(pitch - delta.y, -maxPitch, maxPitch);
        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Move(Vector2 input)
    {
        bool sprinting = !IsCrouching && sprintAction.IsPressed() && input.y > 0.1f;
        float speed = IsCrouching ? crouchSpeed : sprinting ? sprintSpeed : walkSpeed;

        Vector3 target = transform.right * input.x + transform.forward * input.y;
        if (target.sqrMagnitude > 1f) target.Normalize();
        planarVelocity = Vector3.MoveTowards(planarVelocity, target * speed, acceleration * Time.deltaTime);

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        Vector3 actual = controller.velocity;
        actual.y = 0f;
        HorizontalSpeed = actual.magnitude;
    }

    void ToggleCrouch()
    {
        if (IsCrouching)
        {
            // 머리 위가 막혀 있으면 일어서지 않는다
            Vector3 head = transform.position + Vector3.up * (crouchHeight - 0.05f);
            // 플레이어는 Ignore Raycast 레이어라서 자기 콜라이더에는 맞지 않는다
            if (Physics.Raycast(head, Vector3.up, standHeight - crouchHeight + 0.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
        }
        IsCrouching = !IsCrouching;
    }

    void UpdateHeightAndBob()
    {
        float step = crouchTransitionSpeed * Time.deltaTime;
        controller.height = Mathf.MoveTowards(controller.height, IsCrouching ? crouchHeight : standHeight, step);
        controller.center = Vector3.up * (controller.height / 2f);
        eyeHeight = Mathf.MoveTowards(eyeHeight, IsCrouching ? crouchEyeHeight : standEyeHeight, step);

        bool moving = IsGrounded && HorizontalSpeed > 0.1f;
        bobWeight = Mathf.MoveTowards(bobWeight, moving ? 1f : 0f, Time.deltaTime * 4f);
        if (moving) bobDistance += HorizontalSpeed * Time.deltaTime;
        float bob = Mathf.Sin(bobDistance / bobStride * Mathf.PI * 2f) * bobAmplitude * bobWeight;

        cameraRoot.localPosition = Vector3.up * (eyeHeight + bob);
    }
}
