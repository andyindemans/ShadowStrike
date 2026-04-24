using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SpeedFX : MonoBehaviour
{
    public Movement move;
    public Camera cam;

    public float baseFOV = 85f;
    public float maxFOV = 100f;
    public float fovResponsiveness = 8f;
    public float boostDecay = 0.6f;
    public float boostStrength = 0.85f;

    private Rigidbody rb;

    private void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (move != null) rb = move.GetComponent<Rigidbody>();
        cam.fieldOfView = baseFOV;
    }

    private void LateUpdate()
    {
        if (move == null || rb == null || cam == null) return;

        float horizSpeed = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
        float baseline = move.maxSpeed;
        float ceiling = move.maxSpeed * 2f;
        float speedT = Mathf.InverseLerp(baseline, ceiling, horizSpeed);

        float boostT = Mathf.Clamp01(1f - (Time.time - move.lastWallJumpTime) / boostDecay);
        float finalT = Mathf.Max(speedT, boostT * boostStrength);

        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, finalT);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovResponsiveness);
    }
}
