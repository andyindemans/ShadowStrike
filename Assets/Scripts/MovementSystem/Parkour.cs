using UnityEngine;

public class Parkour : MonoBehaviour
{
    //Assignables #===============================#
    public float vaultLerpSpeed = 8f;
    public float vaultForwardReach = 1.5f;
    public float vaultClearance = 0.3f;
    public float vaultMinExitSpeed = 4f;
    public LayerMask whatIsVaultable;

    //Privates
    private Rigidbody rb;
    private Movement movementParams;
    private float playerHeight;
    private float characterHeight;
    private Transform orientation;
    private Vector3 vaultTargetPos;
    private float lastObstacleTopY;
    private float preVaultSpeed;

    //Bools
    private bool grounded;
    public bool isVaulting = false;
    private bool isWallVaultable;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        movementParams = GetComponent<Movement>();
        orientation = movementParams.orientation;
        playerHeight = GetComponent<Renderer>().bounds.size.y * 2.5f;
        characterHeight = GetComponent<Renderer>().bounds.size.y;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Used for RigidBodies
    void FixedUpdate()
    {
        grounded = movementParams.grounded;
        orientation = movementParams.orientation;

        if (isVaulting) { TickVault(); return; }

        CheckForWall();
        if (movementParams.isSprinting && isWallVaultable) StartVault();
    }


    //Parkour moves #=============================#
    private void StartVault()
    {
        if (!grounded) return;

        Vector3 forwardFlat = new Vector3(orientation.forward.x, 0f, orientation.forward.z).normalized;
        vaultTargetPos = ComputeVaultTarget(forwardFlat);

        preVaultSpeed = rb.linearVelocity.magnitude;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        movementParams.StartCrouch(default);
        isVaulting = true;
    }

    // Casts straight down from the forward-reach X/Z above the obstacle top
    // to find the actual landing surface — whether that's the top of a thick
    // obstacle or the ground on the far side of a thin one.
    private Vector3 ComputeVaultTarget(Vector3 forwardFlat)
    {
        Vector3 target = transform.position + forwardFlat * vaultForwardReach;
        Vector3 scanOrigin = new Vector3(target.x, lastObstacleTopY + 2f, target.z);

        float surfaceY = lastObstacleTopY;
        if (Physics.Raycast(scanOrigin, Vector3.down, out RaycastHit hit, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            surfaceY = hit.point.y;

        target.y = surfaceY + characterHeight * 0.5f + vaultClearance;
        return target;
    }

    private void TickVault()
    {
        Vector3 next = Vector3.Lerp(transform.position, vaultTargetPos, vaultLerpSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        if (Vector3.Distance(transform.position, vaultTargetPos) < 0.05f)
            EndVault();
    }

    private void EndVault()
    {
        rb.isKinematic = false;
        Vector3 exitForward = new Vector3(orientation.forward.x, 0f, orientation.forward.z).normalized;
        rb.linearVelocity = exitForward * Mathf.Max(preVaultSpeed, vaultMinExitSpeed);
        movementParams.StopCrouch(default);
        isVaulting = false;
    }

    private void CheckForWall()
    {
        float feetY = transform.position.y - characterHeight * 0.5f;
        float maxObstacleY = feetY + characterHeight * 0.65f;

        // Forward ray at ~15% height (knee/shin) to detect any obstacle directly in front
        Vector3 rayOrigin = new Vector3(transform.position.x, feetY + characterHeight * 0.15f, transform.position.z);
        const float reach = 1.0f;

        Debug.DrawRay(rayOrigin, orientation.forward * reach, Color.yellow);

        if (Physics.Raycast(rayOrigin, orientation.forward, out RaycastHit hit, reach, whatIsVaultable, QueryTriggerInteraction.Ignore))
        {
            // Read the obstacle's AABB top to decide if it's below the 45% threshold
            float obstacleTopY = hit.collider.bounds.max.y;
            float obstacleHeight = obstacleTopY - feetY;
            isWallVaultable = obstacleHeight > 0.1f && obstacleTopY <= maxObstacleY;
            if (isWallVaultable) lastObstacleTopY = obstacleTopY;
        }
        else
        {
            isWallVaultable = false;
        }
    }
}
