using System;
using UnityEngine;

// Flags so WeaponData.allowedAttachmentSlots can be a mask. Individual
// AttachmentData.slot should hold exactly one flag.
[Flags]
public enum AttachmentSlot
{
    None   = 0,
    Barrel = 1 << 0,
    Muzzle = 1 << 1,
    Scope  = 1 << 2,
    Grip   = 1 << 3,
}

public abstract class AttachmentData : ScriptableObject
{
    //Identity #===================#
    public string id;
    public string displayName;
    [TextArea] public string description;
    public AttachmentSlot slot;

    //Additive modifiers #=========#
    public float damageAdd = 0f;
    public float spreadAdd = 0f;
    public float recoilAdd = 0f;
    public float rangeAdd  = 0f;

    //Multiplicative modifiers #===#  (defaults MUST be 1 — additive pass defaults are 0)
    public float damageMult    = 1f;
    public float spreadMult    = 1f;
    public float recoilMult    = 1f;
    public float rangeMult     = 1f;
    public float aimSpeedMult  = 1f;
}
