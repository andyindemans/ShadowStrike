using System.Collections.Generic;
using UnityEngine;

public enum FiringMode
{
    Hitscan,
    Projectile,
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "ShadowStrike/Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    //Identity #===================#
    public string id;
    public string displayName;

    //Firing #=====================#
    public FiringMode firingMode = FiringMode.Hitscan;
    public float fireRate = 6f;          // rounds per second
    public float range = 200f;           // used by hitscan
    public float baseRecoil = 1f;
    public RecoilProfile recoilProfile;

    //Spread (degrees) #===========#
    public float hipSpread = 3f;
    public float adsSpread = 0.25f;

    //Magazine #===================#
    public int magSize = 12;
    public float reloadDuration = 1.5f;

    //Handling #===================#
    public float equipDuration = 0.4f;
    public float holsterDuration = 0.3f;
    public float aimFOV = 50f;
    public float aimMoveSpeedMultiplier = 0.6f;
    public ADSProfile adsProfile;
    public bool oneHanded;
    public SprintPoseProfile sprintPoseProfile;

    //Ammo #=======================#
    public List<AmmoType> acceptedAmmo = new List<AmmoType>();
    public AmmoType defaultAmmo;

    //Visuals / animation #========#
    public GameObject viewModelPrefab;
    public RuntimeAnimatorController animatorOverride;

    //Customization #==============#
    public AttachmentSlot allowedAttachmentSlots = AttachmentSlot.Barrel | AttachmentSlot.Muzzle | AttachmentSlot.Scope;
}
