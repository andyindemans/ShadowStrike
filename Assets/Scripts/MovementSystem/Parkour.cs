using UnityEngine;

public class Parkour : MonoBehaviour
{
    //Assignables #===============================#
    public float vaultSpeed = 5f;
    public LayerMask whatIsVaultable;

    //Privates
    private Rigidbody rb;
    private Movement movementParams;
    private float playerHeight;
    private float characterHeight;
    private Transform orientation;

    //Bools
    private bool grounded;
    private bool isVaulting = false;
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
        Debug.Log($"[Parkour] characterHeight = {characterHeight} (from Renderer '{GetComponent<Renderer>().name}'). Expected ~2 for a standard capsule.");
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

        CheckForWall();
        if (movementParams.isSprinting && isWallVaultable && !isVaulting) Vault();
    }


    //Parkour moves #=============================#
    private void Vault()
    {
        if (grounded)
        {
            // Push player up and forward over the obstacle
            Vector3 vault = new Vector3(orientation.forward.x, playerHeight * 1.5f, orientation.forward.z);
            rb.MovePosition(transform.position + vault * Time.fixedDeltaTime * vaultSpeed);

            movementParams.StartCrouch(default);
            isVaulting = true;
            Invoke(nameof(StopVaultCrouch), 0.5f);
        }
    }

    private void StopVaultCrouch()
    {
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
            if (movementParams.isSprinting)
                Debug.Log($"[Parkour] hit '{hit.collider.name}' layer={LayerMask.LayerToName(hit.collider.gameObject.layer)} obstacleHeight={obstacleHeight:F2} max={characterHeight * 0.45f:F2} vaultable={isWallVaultable}");
        }
        else
        {
            isWallVaultable = false;
            if (movementParams.isSprinting)
                Debug.Log($"[Parkour] no forward obstacle within {reach}m at knee height");
        }
    }
}
