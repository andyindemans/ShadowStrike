using UnityEngine;

// Drives camera recoil (additive offsets on Movement) and visual gun kick
// (local pose on weaponHolder) from the active weapon's RecoilProfile.
//
// Camera kick rides on top of mouse input via Movement.recoilPitchOffset / recoilYawOffset
// so recovery returns toward the player's intent angle without losing player input.
//
// Visual kick is applied to weaponHolder (the parent of the viewmodel) so it doesn't
// fight WeaponMotion, which writes to MotionRig's local pose further up the chain.
public class WeaponRecoil : MonoBehaviour
{
    //Refs #=======================#
    public Movement movement;
    public PlayerInventory inventory;
    public Transform weaponHolder;       // optional — viewmodel parent for visual kick

    //Runtime #====================#
    private Weapon subscribedWeapon;

    // Camera recoil state (degrees, applied to Movement). Pitch is negated when applied
    // because Movement.xRotation is inverted (looking up = more negative).
    private float currentPitchOffset;
    private float currentYawOffset;
    private float permanentPitch;        // residue that never recovers (per-shot * (1 - recoveryFraction))
    private float permanentYaw;
    private float lastShotTime = -999f;

    // Visual kick state (local space) — spring-driven
    private Vector3 visualPosOffset;     // current
    private Vector3 visualRotOffset;     // current (Euler)
    private Vector3 visualPosVelocity;
    private Vector3 visualRotVelocity;
    private Vector3 holderRestPos;
    private Quaternion holderRestRot;

    private void Awake()
    {
        if (movement == null) movement = GetComponentInParent<Movement>();
        if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();
    }

    private void Start()
    {
        if (weaponHolder != null)
        {
            holderRestPos = weaponHolder.localPosition;
            holderRestRot = weaponHolder.localRotation;
        }
    }

    private void OnDisable()
    {
        if (subscribedWeapon != null)
        {
            subscribedWeapon.OnFired -= HandleFired;
            subscribedWeapon = null;
        }
        if (movement != null)
        {
            movement.recoilPitchOffset = 0f;
            movement.recoilYawOffset = 0f;
        }
    }

    private void Update()
    {
        Weapon active = inventory != null ? inventory.Active : null;
        if (active != subscribedWeapon)
        {
            if (subscribedWeapon != null) subscribedWeapon.OnFired -= HandleFired;
            if (active != null) active.OnFired += HandleFired;
            subscribedWeapon = active;

            // Don't carry one weapon's permanent climb into another.
            permanentPitch = 0f;
            permanentYaw = 0f;
            visualPosOffset = Vector3.zero;
            visualRotOffset = Vector3.zero;
            visualPosVelocity = Vector3.zero;
            visualRotVelocity = Vector3.zero;
        }

        var profile = subscribedWeapon != null ? subscribedWeapon.EffectiveStats.recoilProfile : default;
        float dt = Time.deltaTime;
        bool recovering = Time.time - lastShotTime >= profile.recoveryDelay;

        //Camera recovery — exponential decay toward permanent residue
        if (recovering && profile.recoverySpeed > 0f)
        {
            float t = 1f - Mathf.Exp(-profile.recoverySpeed * dt);
            currentPitchOffset = Mathf.Lerp(currentPitchOffset, permanentPitch, t);
            currentYawOffset = Mathf.Lerp(currentYawOffset, permanentYaw, t);
        }

        //Visual kick — semi-implicit Euler spring (always continuous, not gated by recovering)
        float posStiffness = profile.visualPositionStiffness;
        float rotStiffness = profile.visualRotationStiffness;
        float damping = profile.visualDampingRatio;
        float posOmega = Mathf.Sqrt(Mathf.Max(posStiffness, 0f));
        float rotOmega = Mathf.Sqrt(Mathf.Max(rotStiffness, 0f));
        Vector3 posAccel = -posStiffness * visualPosOffset - 2f * damping * posOmega * visualPosVelocity;
        Vector3 rotAccel = -rotStiffness * visualRotOffset - 2f * damping * rotOmega * visualRotVelocity;
        visualPosVelocity += posAccel * dt;
        visualRotVelocity += rotAccel * dt;
        visualPosOffset += visualPosVelocity * dt;
        visualRotOffset += visualRotVelocity * dt;

        if (movement != null)
        {
            movement.recoilPitchOffset = currentPitchOffset;
            movement.recoilYawOffset = currentYawOffset;
        }
        if (weaponHolder != null)
        {
            weaponHolder.localPosition = holderRestPos + visualPosOffset;
            weaponHolder.localRotation = holderRestRot * Quaternion.Euler(visualRotOffset);
        }
    }

    private void HandleFired()
    {
        if (subscribedWeapon == null) return;

        var stats = subscribedWeapon.EffectiveStats;
        var profile = stats.recoilProfile;
        bool aiming = subscribedWeapon.IsAiming;
        float camMult = stats.recoil * Mathf.Max(0f, aiming ? profile.aimCameraRecoilMultiplier : profile.hipCameraRecoilMultiplier);
        float visMult = stats.recoil * (aiming ? Mathf.Max(0f, profile.aimVisualRecoilMultiplier) : 1f);

        float vKick = (profile.verticalKick + Random.Range(-profile.verticalKickVariance, profile.verticalKickVariance)) * camMult;
        float hSign = Random.value < (0.5f + profile.horizontalBias * 0.5f) ? 1f : -1f;
        float hKick = profile.horizontalKick * hSign * camMult;

        if (camMult != 0f)
        {
            //Movement.xRotation is inverted (looking up = more negative), so positive vKick maps to negative offset.
            float pitchDelta = -vKick;
            float yawDelta = hKick;

            currentPitchOffset += pitchDelta;
            currentYawOffset += yawDelta;
            permanentPitch += pitchDelta * (1f - Mathf.Clamp01(profile.recoveryFraction));
            permanentYaw += yawDelta * (1f - Mathf.Clamp01(profile.recoveryFraction));
        }

        visualPosVelocity += profile.visualPositionKick * visMult * profile.visualImpulseScale;
        visualRotVelocity += profile.visualRotationKick * visMult * profile.visualImpulseScale;

        lastShotTime = Time.time;
    }
}
