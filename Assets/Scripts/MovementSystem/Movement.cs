using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    //Assignables #===============================#
    public float maxSlopeAngle = 35f;
    public float runSpeed = 6500f;
    public float walkSpeed = 1000f;
    public float crouchSpeed = 2000f;
    public float sensitivity = 50.0f;
    public Transform playerCam;
    public Transform orientation;
    public float maxSpeed = 7f;
    public float crouchMaxSpeed = 3f;
    public bool enableHeadBobbing = true;
    public Camera camera;

    //Crouch & Slide
    public bool crouching;
    private bool isCrouched;
    private Vector3 playerScale;
    private Vector3 crouchPosition = new Vector3(0, 0.5f, 0);
    public float slideThreshold = 4.5f;
    public float slideExitSpeed = 1.8f;
    private float crouchLerpSpeed = 15f;

    //Privates
    private bool cancellingGrounded;
    private float desiredX;
    private float sensMultiplier = 1f;
    private float xRotation;

    //Recoil — written by WeaponRecoil each frame; added on top of mouse-driven angles in Look().
    [HideInInspector] public float recoilPitchOffset;
    [HideInInspector] public float recoilYawOffset;
    private Vector3 direction;
    private bool isRunning;

    //Head Bobbing
    private float runBobSpeed = 12f;
    [Range(0.01f, 0.1f)] public float runBobAmount = 0.05f;
    private float defaultYPos = 0;
    private float timer;
    [HideInInspector] public float bobOffset;   // current head-bob Y delta from defaultYPos — read by WeaponSway

    //Input
    private Rigidbody rb;
    private MovementSystem movementSystem;
    public MovementSystem InputSystem => movementSystem;

    //Bools
    public bool isSprinting = false;
    public bool grounded;
    public bool isSliding;

    //Jumping
    public bool jumping;
    private bool readyToJump = true;
    public float jumpForce = 450f;

    //Sliding
    public float slideForce = 400f;
    public float slideCounterMovement = 0.08f;
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;
    private float counterMovement = 0.1f;
    private float threshold = 0.03f;

    //Wallrunning
    private float wallRunCameraTilt = 0f;
    public LayerMask whatIsWall;
    public float wallrunForce = 3500;
    public float maxWallSpeed = 7;
    bool isWallRight, isWallLeft;
    public bool isWallRunning;
    private bool wallrunButtonHeld = false;
    public float maxWallRunCameraTilt = 15;
    [HideInInspector] public float lastWallJumpTime = -999f;

    //Animations
    public Animator animator;
    public bool allowRunAnimation = false;



    //Application methods #=======================#
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Frictionless player collider with Minimum combine so contact friction
        // is min(0, wall) = 0 regardless of what the wall has — lets the player
        // glide along any surface instead of getting dragged to a stop by default
        // Unity friction on colliders without a physics material.
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.sharedMaterial = new PhysicsMaterial("PlayerFrictionless")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
        }

        movementSystem = new MovementSystem();
        movementSystem.Movement.Enable();

        //Jumping — suppressed when near a wall (Space used for wallrun there)
        movementSystem.Movement.Jump.performed += Jump;

        //Crouching & Sliding
        movementSystem.Movement.Crouch.performed += StartCrouch;
        movementSystem.Movement.Crouch.canceled += StopCrouch;

        //Wallrunning
        movementSystem.Movement.Wallrun.performed += ctx => wallrunButtonHeld = true;
        movementSystem.Movement.Wallrun.canceled += WallJump;

        //Sprinting
        movementSystem.Movement.Sprint.performed += context => isSprinting = true;
        movementSystem.Movement.Sprint.canceled += context => isSprinting = false;

        //Head Bobbing
        defaultYPos = camera.transform.localPosition.y;

        //Seed yaw accumulator from current camera rotation so spawn orientation is preserved.
        desiredX = playerCam.localRotation.eulerAngles.y;
    }

    void Start()
    {
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        Look();
        CheckForWall();

    }

    private void FixedUpdate()
    {
        if (!isWallRunning) Move();
        if (enableHeadBobbing) HeadBob();
        if ((isWallRight || isWallLeft) && wallrunButtonHeld) Wallrun();

        isRunning = rb.linearVelocity.magnitude > 0.5f;
        isCrouched = grounded && crouching;
        if (isSliding) isSliding = rb.linearVelocity.magnitude > slideExitSpeed;

        AnimatePlayerScale();
    }


    //Ground Detection #==========================#
    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }


    private void OnCollisionStay(Collision other)
    {
        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            if (IsFloor(normal))
            {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                ResetJump();
                CancelInvoke(nameof(StopGrounded));
            }
        }

        float delay = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    private void StopGrounded()
    {
        grounded = false;
    }


    //Movement Functions #========================#
    public void Move()
    {
        rb.AddForce(Vector3.down * Time.fixedDeltaTime * 10);

        Vector2 inputVector = movementSystem.Movement.Walk.ReadValue<Vector2>();

        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        CounterMovement(inputVector.x, inputVector.y, mag);

        // Determine speed and velocity cap based on state
        float activeSpeed;
        float activeMaxSpeed;
        if (isCrouched && !isSliding)
        {
            activeSpeed = crouchSpeed;
            activeMaxSpeed = crouchMaxSpeed;
        }
        else if (isSprinting && !isCrouched)
        {
            activeSpeed = runSpeed;
            activeMaxSpeed = maxSpeed;
        }
        else
        {
            activeSpeed = walkSpeed;
            activeMaxSpeed = maxSpeed;
        }

        if (inputVector.x > 0 && xMag > activeMaxSpeed) inputVector.x = 0;
        if (inputVector.x < 0 && xMag < -activeMaxSpeed) inputVector.x = 0;
        if (inputVector.y > 0 && yMag > activeMaxSpeed) inputVector.y = 0;
        if (inputVector.y < 0 && yMag < -activeMaxSpeed) inputVector.y = 0;

        float multiplier = 1f;
        if (!grounded && !isWallRunning) multiplier = 0.1f;

        if (isSliding) return;

        direction = new Vector3(inputVector.x, 0, inputVector.y);
        direction = orientation.transform.TransformDirection(direction);
        rb.AddForce(direction * activeSpeed * Time.fixedDeltaTime * multiplier, ForceMode.Force);
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || jumping) return;

        if (isSliding)
        {
            rb.AddForce(runSpeed * Time.fixedDeltaTime * -rb.linearVelocity.normalized * slideCounterMovement);
            return;
        }

        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(runSpeed * orientation.transform.right * Time.fixedDeltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(runSpeed * orientation.transform.forward * Time.fixedDeltaTime * -mag.y * counterMovement);
        }

        //Limit diagonal running
        if (Mathf.Sqrt((Mathf.Pow(rb.linearVelocity.x, 2) + Mathf.Pow(rb.linearVelocity.z, 2))) > maxSpeed)
        {
            float fallspeed = rb.linearVelocity.y;
            Vector3 n = rb.linearVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(n.x, fallspeed, n.z);
        }

    }

    public void Jump(InputAction.CallbackContext context)
    {
        // Space also triggers wallrun — suppress jump when a wall is detected
        if (isWallRight || isWallLeft) return;

        if (grounded && readyToJump && !isWallRunning && !crouching)
        {
            Vector3 jump = new Vector3(0, jumpForce * 0.65f, 0);

            //Add jump forces
            rb.AddForce(jump * Time.fixedDeltaTime, ForceMode.Impulse);

            readyToJump = false;

            Invoke(nameof(ResetJump), 0.2f);
        }

    }


    private void ResetJump()
    {
        readyToJump = true;
    }


    public void StartCrouch(InputAction.CallbackContext context)
    {
        crouching = true;
        // Enter slide only when moving fast enough (sprint into crouch), otherwise crouch-walk
        isSliding = rb.linearVelocity.magnitude > slideThreshold && grounded;

        if (isSliding)
        {
            rb.AddForce(orientation.transform.forward * slideForce);
        }
    }

    public void StopCrouch(InputAction.CallbackContext context)
    {
        crouching = false;
    }

    private void AnimatePlayerScale()
    {
        Vector3 targetScale = crouching ? crouchPosition : playerScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, crouchLerpSpeed * Time.fixedDeltaTime);
    }


    private void Look()
    {
        Vector2 mouseDelta = movementSystem.Movement.MouseLook.ReadValue<Vector2>();
        float mouseX = mouseDelta.x * sensitivity * Time.deltaTime * sensMultiplier;
        float mouseY = mouseDelta.y * sensitivity * Time.deltaTime * sensMultiplier;

        //Accumulate mouse intent. Recoil rides on top via recoilPitchOffset / recoilYawOffset
        //so it can recover toward the player's intended aim without losing player input.
        desiredX += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 65f);

        //Perform the rotations
        playerCam.transform.localRotation = Quaternion.Euler(xRotation + recoilPitchOffset, desiredX + recoilYawOffset, wallRunCameraTilt);
        orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);

        //Wallrunning camera tilt
        if (Math.Abs(wallRunCameraTilt) < maxWallRunCameraTilt && isWallRunning && isWallRight)
            wallRunCameraTilt += Time.deltaTime * maxWallRunCameraTilt * 5;
        if (Math.Abs(wallRunCameraTilt) < maxWallRunCameraTilt && isWallRunning && isWallLeft)
            wallRunCameraTilt -= Time.deltaTime * maxWallRunCameraTilt * 5;

        //Tilts camera back again
        if (wallRunCameraTilt > 0 && !isWallRight && !isWallLeft)
            wallRunCameraTilt -= Time.deltaTime * maxWallRunCameraTilt * 2;
        if (wallRunCameraTilt < 0 && !isWallRight && !isWallLeft)
            wallRunCameraTilt += Time.deltaTime * maxWallRunCameraTilt * 2;
    }

    private void HeadBob()
    {
        if (!grounded || crouching)
        {
            allowRunAnimation = false;
            return;
        }

        if (isRunning)
        {
            timer += Time.deltaTime * runBobSpeed;
            bobOffset = Mathf.Sin(timer) * runBobAmount;
            camera.transform.localPosition = new Vector3(camera.transform.localPosition.x, defaultYPos + bobOffset, camera.transform.localPosition.z);
            allowRunAnimation = true;
        } else
        {
            allowRunAnimation = false;
        }
    }

    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitude = rb.linearVelocity.magnitude;
        float yMag = magnitude * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitude * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }




    //Wallrunning component #========================================#
    private void Wallrun()
    {
        if (!grounded)
        {
            rb.useGravity = false;
            isWallRunning = true;

            if (rb.linearVelocity.magnitude <= maxWallSpeed)
            {
                //Negate upward motion, but keep forward momentum
                Vector3 momentum = new Vector3(orientation.forward.x, 0, orientation.forward.z);
                rb.AddForce(momentum * wallrunForce * Time.fixedDeltaTime);

                if (isWallRight)
                    rb.AddForce(orientation.right * wallrunForce / 7 * Time.fixedDeltaTime);
                else
                    rb.AddForce(-orientation.right * wallrunForce / 7 * Time.fixedDeltaTime);
            }
        } 
    }
    private void StopWallRun()
    {
        isWallRunning = false;
        rb.useGravity = true;
    }
    private void CheckForWall()
    {
        isWallRight = Physics.Raycast(transform.position, orientation.right, 1f, whatIsWall);
        isWallLeft = Physics.Raycast(transform.position, -orientation.right, 1f, whatIsWall);

        if (!isWallLeft && !isWallRight) StopWallRun();
    }

    private void WallJump(InputAction.CallbackContext context)
    {
        wallrunButtonHeld = false;

        if (isWallRunning)
        {
            StopWallRun(); // restores gravity immediately

            readyToJump = false;
            Vector3 jump = new Vector3(0, jumpForce * 0.5f, 0);

            //Leap forwards when jumping off a wall
            rb.AddForce(orientation.forward * (jumpForce * 0.7f) * Time.fixedDeltaTime, ForceMode.Impulse);
            rb.AddForce(jump * Time.fixedDeltaTime, ForceMode.Impulse);

            if (isWallLeft)
            {
                rb.AddForce(orientation.right * (jumpForce * 0.3f) * Time.fixedDeltaTime, ForceMode.Impulse);
            }
            else
            {
                rb.AddForce(-orientation.right * (jumpForce * 0.3f) * Time.fixedDeltaTime, ForceMode.Impulse);
            }

            lastWallJumpTime = Time.time;
        }
    }
}
