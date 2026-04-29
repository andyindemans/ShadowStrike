using UnityEngine;

public class WeaponMotion : MonoBehaviour
{
    const float LookSwayPosToPitchDeg = 60f;
    const float LookSwayRollFactor = 0.5f;

    //Refs #=======================#
    public Movement movement;
    public PlayerInventory inventory;

    //Runtime #====================#
    private Rigidbody playerRb;
    private Vector3 restLocalPos;
    private Quaternion restLocalRot;
    private float bobPhase;
    private float perlinTime;
    private Vector3 smoothBobPos;
    private Vector3 smoothBobPosVel;
    private Vector3 smoothBobRot;
    private Vector3 smoothBobRotVel;
    private float breathPhase;
    private float breathWeight = 1f;
    private Vector3 swayPos;
    private Vector3 swayPosVel;
    private Vector3 swayRotEuler;
    private Vector3 swayRotVel;

    private void Awake()
    {
        restLocalPos = transform.localPosition;
        restLocalRot = transform.localRotation;

        if (movement == null) movement = GetComponentInParent<Movement>();
        if (movement == null) movement = FindFirstObjectByType<Movement>();
        if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();
        if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
        playerRb = movement != null ? movement.GetComponent<Rigidbody>() : null;
    }

    private void LateUpdate()
    {
        var active = inventory != null ? inventory.Active : null;
        var profile = active != null ? active.EffectiveStats.motionProfile : WeaponMotionProfile.Default;

        Vector3 bobPos = Vector3.zero;
        Vector3 bobRot = Vector3.zero;
        Vector3 breathPos = Vector3.zero;
        Vector3 breathRot = Vector3.zero;

        Vector3 planar = playerRb != null
            ? new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z)
            : Vector3.zero;

        perlinTime += Time.deltaTime * profile.walkBobNoiseFrequency;

        if (movement != null && movement.grounded && !movement.crouching && playerRb != null)
        {
            float speedNorm = Mathf.Clamp01(planar.magnitude / Mathf.Max(0.01f, movement.maxSpeed));

            if (planar.magnitude > profile.walkBobMinSpeed)
            {
                float adsScale = (active != null && active.IsAiming)
                    ? Mathf.Max(0f, active.EffectiveStats.aimMoveSpeedMultiplier)
                    : 1f;

                bobPhase += Time.deltaTime * profile.walkBobBaseFrequency * speedNorm * adsScale * Mathf.PI * 2f;

                float modPosX = 1f + (Mathf.PerlinNoise(perlinTime, 0f) * 2f - 1f) * profile.walkBobVariance;
                float modPosY = 1f + (Mathf.PerlinNoise(perlinTime, 13.7f) * 2f - 1f) * profile.walkBobVariance;
                float modPosZ = 1f + (Mathf.PerlinNoise(perlinTime, 27.4f) * 2f - 1f) * profile.walkBobVariance;
                float modRotX = 1f + (Mathf.PerlinNoise(perlinTime, 41.1f) * 2f - 1f) * profile.walkBobVariance;
                float modRotY = 1f + (Mathf.PerlinNoise(perlinTime, 54.8f) * 2f - 1f) * profile.walkBobVariance;
                float modRotZ = 1f + (Mathf.PerlinNoise(perlinTime, 68.5f) * 2f - 1f) * profile.walkBobVariance;

                bobPos = new Vector3(
                    Mathf.Sin(bobPhase) * profile.walkBobPosAmplitude.x * modPosX,
                    -Mathf.Abs(Mathf.Sin(bobPhase)) * profile.walkBobPosAmplitude.y * modPosY,
                    Mathf.Sin(bobPhase * 0.5f) * profile.walkBobPosAmplitude.z * modPosZ
                ) * speedNorm * adsScale;

                bobRot = new Vector3(
                    Mathf.Sin(bobPhase) * profile.walkBobRotAmplitude.x * modRotX,
                    Mathf.Sin(bobPhase * 0.5f) * profile.walkBobRotAmplitude.y * modRotY,
                    Mathf.Sin(bobPhase) * profile.walkBobRotAmplitude.z * modRotZ
                ) * speedNorm * adsScale;
            }
            else
            {
                bobPhase = Mathf.Lerp(bobPhase, 0f, 1f - Mathf.Exp(-12f * Time.deltaTime));
            }
        }
        else
        {
            bobPhase = Mathf.Lerp(bobPhase, 0f, 1f - Mathf.Exp(-12f * Time.deltaTime));
        }

        float omega = 2f * Mathf.PI * profile.walkBobSmoothFrequency;
        float k = omega * omega;
        IntegrateSpring(ref smoothBobPos, ref smoothBobPosVel, bobPos, k, profile.walkBobSmoothDamping, Time.deltaTime);
        IntegrateSpring(ref smoothBobRot, ref smoothBobRotVel, bobRot, k, profile.walkBobSmoothDamping, Time.deltaTime);

        breathPhase += Time.deltaTime * profile.idleBreathFrequency * Mathf.PI * 2f;

        float targetBreathWeight = (movement != null && movement.grounded && planar.magnitude < profile.walkBobMinSpeed) ? 1f : 0f;
        breathWeight = Mathf.Lerp(breathWeight, targetBreathWeight, 1f - Mathf.Exp(-profile.idleBreathMoveFadeSpeed * Time.deltaTime));

        float breathAdsScale = (active != null && active.IsAiming) ? profile.idleBreathADSScale : 1f;

        breathPos = Vector3.Scale(
            new Vector3(Mathf.Cos(breathPhase * 0.5f), Mathf.Sin(breathPhase), Mathf.Sin(breathPhase * 0.3f)),
            profile.idleBreathAmplitude
        ) * breathWeight * breathAdsScale;

        breathRot = new Vector3(
            Mathf.Sin(breathPhase) * profile.idleBreathRotAmplitude.x,
            Mathf.Sin(breathPhase * 0.7f) * profile.idleBreathRotAmplitude.y,
            0f
        ) * breathWeight * breathAdsScale;

        Vector2 mouseDelta = movement != null
            ? movement.InputSystem.Movement.MouseLook.ReadValue<Vector2>()
            : Vector2.zero;

        Vector3 targetSwayPos = new Vector3(
            -mouseDelta.x * profile.lookSwaySensitivity.x,
            -mouseDelta.y * profile.lookSwaySensitivity.y,
            0f);

        Vector3 targetSwayRot = new Vector3(
             mouseDelta.y * profile.lookSwaySensitivity.y * LookSwayPosToPitchDeg,
             mouseDelta.x * profile.lookSwaySensitivity.x * LookSwayPosToPitchDeg,
            -mouseDelta.x * profile.lookSwaySensitivity.x * LookSwayPosToPitchDeg * LookSwayRollFactor);

        if (active != null && active.IsAiming)
        {
            targetSwayPos *= 0.5f;
            targetSwayRot *= 0.5f;
        }

        targetSwayPos.x = Mathf.Clamp(targetSwayPos.x, -profile.lookSwayMaxOffset.x, profile.lookSwayMaxOffset.x);
        targetSwayPos.y = Mathf.Clamp(targetSwayPos.y, -profile.lookSwayMaxOffset.y, profile.lookSwayMaxOffset.y);
        targetSwayPos.z = Mathf.Clamp(targetSwayPos.z, -profile.lookSwayMaxOffset.z, profile.lookSwayMaxOffset.z);
        targetSwayRot.x = Mathf.Clamp(targetSwayRot.x, -profile.lookSwayMaxRot.x, profile.lookSwayMaxRot.x);
        targetSwayRot.y = Mathf.Clamp(targetSwayRot.y, -profile.lookSwayMaxRot.y, profile.lookSwayMaxRot.y);
        targetSwayRot.z = Mathf.Clamp(targetSwayRot.z, -profile.lookSwayMaxRot.z, profile.lookSwayMaxRot.z);

        IntegrateSpring(ref swayPos, ref swayPosVel, targetSwayPos, profile.lookSwayStiffness, profile.lookSwayDampingRatio, Time.deltaTime);
        IntegrateSpring(ref swayRotEuler, ref swayRotVel, targetSwayRot, profile.lookSwayStiffness, profile.lookSwayDampingRatio, Time.deltaTime);

        transform.localPosition = restLocalPos + smoothBobPos + breathPos + swayPos;
        transform.localRotation = restLocalRot * Quaternion.Euler(smoothBobRot + breathRot + swayRotEuler);
    }

    static void IntegrateSpring(ref Vector3 pos, ref Vector3 vel, Vector3 target, float k, float zeta, float dt)
    {
        float omega = Mathf.Sqrt(Mathf.Max(0.001f, k));
        float fx = -k * (pos.x - target.x) - 2f * zeta * omega * vel.x;
        float fy = -k * (pos.y - target.y) - 2f * zeta * omega * vel.y;
        float fz = -k * (pos.z - target.z) - 2f * zeta * omega * vel.z;
        vel.x += fx * dt; vel.y += fy * dt; vel.z += fz * dt;
        pos.x += vel.x * dt; pos.y += vel.y * dt; pos.z += vel.z * dt;
    }
}
