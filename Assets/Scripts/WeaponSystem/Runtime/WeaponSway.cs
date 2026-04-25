using UnityEngine;

// A small, self-contained walk bob for the weapon viewmodel,
// independent of the camera's transform. Reads Movement + Rigidbody state
// directly so jumps, wall-runs, and camera-rig hierarchy quirks don't leak in.
public class WeaponSway : MonoBehaviour
{
    //Refs #=======================#
    public Movement movement;
    public PlayerInventory inventory;    // used to scale bob by the active weapon's ADS speed multiplier

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
        if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();
        if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
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
                // Scale frequency + amplitude by the active weapon's ADS multiplier so the
                // bob tracks the player's actual movement speed when aiming.
                float speedScale = 1f;
                var active = inventory != null ? inventory.Active : null;
                if (active != null && active.IsAiming)
                {
                    speedScale = Mathf.Max(0f, active.EffectiveStats.aimMoveSpeedMultiplier);
                }

                timer += Time.deltaTime * bobSpeed * speedScale;
                targetY = restLocalPos.y + Mathf.Sin(timer) * bobAmount * speedScale;
            }
        }

        Vector3 p = transform.localPosition;
        p.y = Mathf.Lerp(p.y, targetY, Time.deltaTime * smoothing);
        transform.localPosition = p;
    }
}
