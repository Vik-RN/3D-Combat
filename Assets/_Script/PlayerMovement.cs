using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerEnergy))]
public class PlayerMovement : MonoBehaviour
{
    public enum MovementState { Grounded, MidAir, Dashing }

    [Header("State Machine")]
    public MovementState currentState = MovementState.Grounded;

    [Header("References")]
    private CharacterController controller;
    private PlayerEnergy energyManager;
    private PlayerControls playerControls;
    public Transform cameraTransform; 
    public Animator animator;

    [Header("Weapon Models")]
    [Tooltip("Masukkan objek Sword_Hand yang ada di tulang tangan")]
    public GameObject swordHand;
    [Tooltip("Masukkan objek Sword_Hip yang ada di tulang pinggang")]
    public GameObject swordHip;

    [Header("Animation & Combat Status")]
    public bool isCombatMode = false; 
    private bool lastCombatModeState; // Untuk mendeteksi perubahan mode di Inspector
    
    private string currentAnim;       
    private bool useDash2 = false;    
    private bool useAirJump2 = false; 
    private float animLockTimer = 0f; 

    [Header("Movement Settings")]
    public float moveSpeed = 7f;
    public float rotationSpeed = 15f;
    public float gravity = -20f;
    private Vector3 velocity;
    private Vector2 moveInput;

    [Header("Jump Settings")]
    public float jumpHeight = 3f;
    public int maxAirJumps = 5; 
    private int airJumpCount = 0;

    [Header("Dash Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f; 
    public float dashAnimDuration = 0.6f; 
    private float dashTimer;
    private Vector3 dashDirection;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        energyManager = GetComponent<PlayerEnergy>();
        playerControls = new PlayerControls();
    }

    void OnEnable()
    {
        playerControls.Gameplay.Enable();
        playerControls.Gameplay.Jump.performed += OnJumpPerformed;
        playerControls.Gameplay.Dash.performed += OnDashPerformed;
    }

    void OnDisable()
    {
        playerControls.Gameplay.Jump.performed -= OnJumpPerformed;
        playerControls.Gameplay.Dash.performed -= OnDashPerformed;
        playerControls.Gameplay.Disable();
    }

    void Start()
    {
        // Set visibilitas senjata awal saat game baru dimulai
        lastCombatModeState = isCombatMode;
        UpdateWeaponVisibility();
    }

    void Update()
    {
        moveInput = playerControls.Gameplay.Move.ReadValue<Vector2>();

        if (animLockTimer > 0)
        {
            animLockTimer -= Time.deltaTime;
        }

        // Jika ada perubahan centang pada isCombatMode (misal diklik via Inspector)
        if (isCombatMode != lastCombatModeState)
        {
            UpdateWeaponVisibility();
            lastCombatModeState = isCombatMode;
        }

        switch (currentState)
        {
            case MovementState.Grounded:
                HandleGroundedState();
                break;
            case MovementState.MidAir:
                HandleMidAirState();
                break;
            case MovementState.Dashing:
                HandleDashingState();
                break;
        }
    }

    void HandleGroundedState()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
            airJumpCount = 0;
        }

        MoveCharacter(moveSpeed);
        ApplyGravity();

        if (animLockTimer <= 0)
        {
            if (moveInput.magnitude > 0.1f) 
            {
                ChangeAnimation(isCombatMode ? "Run_SwordHand_MC" : "Run_SwordHip_MC");
            }
            else 
            {
                ChangeAnimation(isCombatMode ? "Idle_SwdHd_MC" : "Idle_MC");
            }
        }

        if (!controller.isGrounded)
        {
            currentState = MovementState.MidAir;
        }
    }

    void HandleMidAirState()
    {
        MoveCharacter(moveSpeed);
        ApplyGravity();

        if (controller.isGrounded)
        {
            currentState = MovementState.Grounded;
        }
    }

    void HandleDashingState()
    {
        controller.Move(dashDirection * dashSpeed * Time.deltaTime);

        dashTimer -= Time.deltaTime;
        if (dashTimer <= 0)
        {
            currentState = controller.isGrounded ? MovementState.Grounded : MovementState.MidAir;
        }
    }

    void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (currentState == MovementState.Dashing) return;

        if (currentState == MovementState.Grounded)
        {
            ExecuteJump();
        }
        else if (currentState == MovementState.MidAir && airJumpCount < maxAirJumps)
        {
            ExecuteJump();
            airJumpCount++;
        }
    }

    void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (currentState != MovementState.Dashing && energyManager.CanDash())
        {
            energyManager.UseDashEnergy();
            StartDash();
        }
    }

    void MoveCharacter(float speed)
    {
        if (cameraTransform == null) return;

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 moveDirection = camForward * moveInput.y + camRight * moveInput.x;
        bool isHoldingRightClick = playerControls.Gameplay.CameraPanHold.ReadValue<float>() > 0.1f;

        if (isHoldingRightClick)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        controller.Move(moveDirection * speed * Time.deltaTime);
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void ExecuteJump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        
        if (currentState == MovementState.Grounded)
        {
            currentState = MovementState.MidAir;
            ChangeAnimation(isCombatMode ? "Jump_SwordHand_MC" : "Jump_SwordHip_MC");
        }
        else 
        {
            if (isCombatMode) {
                ChangeAnimation(useAirJump2 ? "JumpAir_SwdHd2_MC" : "JumpAir_SwdHd1_MC");
            } else {
                ChangeAnimation(useAirJump2 ? "JumpAir_SwordHip2_MC" : "JumpAir_SwordHip1_MC");
            }
            useAirJump2 = !useAirJump2; 
        }
    }

    void StartDash()
    {
        currentState = MovementState.Dashing;
        dashTimer = dashDuration;
        animLockTimer = dashAnimDuration; 

        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 moveDirection = camForward * moveInput.y + camRight * moveInput.x;
            dashDirection = moveDirection.magnitude > 0 ? moveDirection.normalized : transform.forward;
        }
        else
        {
            dashDirection = transform.forward;
        }

        velocity.y = 0; 

        if (isCombatMode) {
            ChangeAnimation(useDash2 ? "Dash2_SwordHand_MC" : "Dash1_SwordHand_MC");
        } else {
            ChangeAnimation(useDash2 ? "Dash2_SwordHip_MC" : "Dash1_SwordHip_MC");
        }
        useDash2 = !useDash2; 
    }

    void ChangeAnimation(string newAnim, float transitionDuration = 0.1f)
    {
        if (animator == null) return;
        if (currentAnim == newAnim) return;

        animator.CrossFadeInFixedTime(newAnim, transitionDuration);
        currentAnim = newAnim;
    }

    // Fungsi baru untuk mengatur nyala/mati pedang
    void UpdateWeaponVisibility()
    {
        if (swordHand != null) swordHand.SetActive(isCombatMode);
        if (swordHip != null) swordHip.SetActive(!isCombatMode);
    }
}