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
    }

    // Update is called once per frame
    void Update()
    {
        CheckForWall();
        if (isWallVaultable && !isVaulting) Vault();
    }

    //Used for RigidBodies
    void FixedUpdate()
    {
        grounded = movementParams.grounded;
        orientation = movementParams.orientation;
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
        Vector3 rayHeight = new Vector3(transform.position.x, transform.position.y - 0.7f, transform.position.z);
        isWallVaultable = Physics.Raycast(rayHeight, orientation.forward, 0.75f, whatIsVaultable);

    }
}
