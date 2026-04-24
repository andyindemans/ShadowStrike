using UnityEngine;

// Generates a small, self-contained walk bob for the weapon viewmodel,
// independent of the camera's transform. Reads Movement + Rigidbody state
// directly so jumps, wall-runs, and camera-rig hierarchy quirks don't leak in.
public class WeaponSway : MonoBehaviour
{
    //Refs #=======================#
    public Movement movement;            // auto-found at Awake if null

    //Walk bob #===================#
    public float bobAmount = 0.015f;     // half the camera amplitude by default
    public float bobSpeed = 12f;         // cycles — match or approach Movement.runBobSpeed for a synced feel
    public float moveThreshold = 0.5f;   // same as Movement.isRunning gate

    //Smoothing #==================#
    public float smoothing = 10f;

    //Runtime
    private Vector3 restLocalPos;
    private Rigidbody playerRb;
    private float timer;

    private void Awake()
    {
        restLocalPos = transform.localPosition;

        if (movement == null) movement = GetComponentInParent<Movement>();
        if (movement == null) movement = FindFirstObjectByType<Movement>();
        if (movement != null) playerRb = movement.GetComponent<Rigidbody>();
    }

    private void LateUpdate()
    {
        float targetY = restLocalPos.y;

        if (movement != null && playerRb != null)
        {
            bool moving = playerRb.linearVelocity.magnitude > moveThreshold;
            bool bobbing = movement.grounded && !movement.crouching && moving;
            if (bobbing)
            {
                timer += Time.deltaTime * bobSpeed;
                targetY = restLocalPos.y + Mathf.Sin(timer) * bobAmount;
            }
        }

        Vector3 p = transform.localPosition;
        p.y = Mathf.Lerp(p.y, targetY, Time.deltaTime * smoothing);
        transform.localPosition = p;
    }
}
