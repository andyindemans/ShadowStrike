using UnityEngine;

// Drives an AimRig transform's local pose toward the active weapon's ADSProfile when aiming.
//
// AimRig sits between PlayerCam and WeaponHolder so this composes with WeaponRecoil
// (which owns weaponHolder's local pose) and WeaponMotion (which owns MotionRig's
// local pose) without fighting them — each transform has a single writer.
public class WeaponADSPose : MonoBehaviour
{
    //Refs #=======================#
    public PlayerInventory inventory;
    public Transform aimRig;

    //Runtime #====================#
    private Vector3 restPos;
    private Quaternion restRot;
    private bool restCached;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();
    }

    private void Start()
    {
        if (aimRig != null)
        {
            restPos = aimRig.localPosition;
            restRot = aimRig.localRotation;
            restCached = true;
        }
    }

    private void Update()
    {
        if (aimRig == null || !restCached) return;

        Weapon active = inventory != null ? inventory.Active : null;
        ADSProfile profile = active != null ? active.EffectiveStats.adsProfile : default;
        bool aiming = active != null && active.IsAiming;

        Vector3 targetPos = aiming ? restPos + profile.aimPositionOffset : restPos;
        Quaternion targetRot = aiming ? restRot * Quaternion.Euler(profile.aimRotationOffset) : restRot;

        if (profile.adsLerpSpeed > 0f)
        {
            float t = 1f - Mathf.Exp(-profile.adsLerpSpeed * Time.deltaTime);
            aimRig.localPosition = Vector3.Lerp(aimRig.localPosition, targetPos, t);
            aimRig.localRotation = Quaternion.Slerp(aimRig.localRotation, targetRot, t);
        }
        else
        {
            aimRig.localPosition = targetPos;
            aimRig.localRotation = targetRot;
        }
    }
}
