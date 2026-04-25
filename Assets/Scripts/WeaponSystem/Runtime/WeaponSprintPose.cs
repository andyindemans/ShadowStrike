using UnityEngine;

// Drives a SprintRig transform's local pose toward the active weapon's SprintPoseProfile
// while the player is sprinting and firingWhileSprintingAllowed is off.
//
// SprintRig sits between AimRig and WeaponHolder so this composes with WeaponADSPose
// (which owns AimRig) and WeaponRecoil (which owns WeaponHolder) without fighting them
// — each transform has a single writer.
public class WeaponSprintPose : MonoBehaviour
{
    //Refs #=======================#
    public PlayerInventory inventory;
    public Movement movement;
    public WeaponController controller;
    public Transform sprintRig;

    //Runtime #====================#
    private Vector3 restPos;
    private Quaternion restRot;
    private bool restCached;

    private void Awake()
    {
        if (inventory == null)  inventory  = GetComponentInParent<PlayerInventory>();
        if (movement == null)   movement   = GetComponentInParent<Movement>();
        if (controller == null) controller = GetComponentInParent<WeaponController>();
    }

    private void Start()
    {
        if (sprintRig != null)
        {
            restPos = sprintRig.localPosition;
            restRot = sprintRig.localRotation;
            restCached = true;
        }
    }

    private void Update()
    {
        if (sprintRig == null || !restCached) return;

        Weapon active = inventory != null ? inventory.Active : null;
        SprintPoseProfile profile = active != null ? active.EffectiveStats.sprintPoseProfile : default;

        bool allowSprintFire = controller != null && controller.firingWhileSprintingAllowed;
        bool sprinting = movement != null && movement.isSprinting;
        bool aiming = active != null && active.IsAiming;
        bool shouldPose = active != null && sprinting && !allowSprintFire && !aiming;

        Vector3 targetPos = restPos;
        Quaternion targetRot = restRot;

        if (shouldPose)
        {
            float phase = Time.time * profile.bobFrequency * Mathf.PI * 2f;
            float s = Mathf.Sin(phase);
            float s2 = Mathf.Sin(phase * 2f);

            //Two-axis Lissajous-ish bob: y rides the step, x/z drift on a half-rate
            Vector3 posBob = new Vector3(
                profile.bobPositionAmplitude.x * s2,
                profile.bobPositionAmplitude.y * s,
                profile.bobPositionAmplitude.z * s2);

            Vector3 rotBob = new Vector3(
                profile.bobRotationAmplitude.x * s,
                profile.bobRotationAmplitude.y * s2,
                profile.bobRotationAmplitude.z * s);

            targetPos = restPos + profile.sprintPositionOffset + posBob;
            targetRot = restRot * Quaternion.Euler(profile.sprintRotationOffset + rotBob);
        }

        if (profile.lerpSpeed > 0f)
        {
            float t = 1f - Mathf.Exp(-profile.lerpSpeed * Time.deltaTime);
            sprintRig.localPosition = Vector3.Lerp(sprintRig.localPosition, targetPos, t);
            sprintRig.localRotation = Quaternion.Slerp(sprintRig.localRotation, targetRot, t);
        }
        else
        {
            sprintRig.localPosition = targetPos;
            sprintRig.localRotation = targetRot;
        }
    }
}
