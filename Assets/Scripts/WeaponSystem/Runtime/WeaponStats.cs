using System.Collections.Generic;

// Resolved weapon stats after applying all attachments + ammo. Plain struct; zero-alloc.
public struct WeaponStats
{
    public float damage;
    public float hipSpread;
    public float adsSpread;
    public float range;
    public float recoil;
    public float aimFOV;
    public float aimMoveSpeedMultiplier;
    public float fireRate;
    public RecoilProfile recoilProfile;

    public static WeaponStats Resolve(WeaponData data, IReadOnlyList<AttachmentData> attachments, AmmoType ammo)
    {
        WeaponStats s = default;
        if (data == null) return s;

        s.damage = ammo != null ? ammo.baseDamage : 0f;
        s.hipSpread = data.hipSpread;
        s.adsSpread = data.adsSpread;
        s.range = data.range;
        s.recoil = data.baseRecoil;
        s.aimFOV = data.aimFOV;
        s.aimMoveSpeedMultiplier = data.aimMoveSpeedMultiplier;
        s.fireRate = data.fireRate;
        s.recoilProfile = data.recoilProfile;

        if (attachments == null || attachments.Count == 0) return s;

        //Additive pass
        for (int i = 0; i < attachments.Count; i++)
        {
            var a = attachments[i];
            if (a == null) continue;
            s.damage    += a.damageAdd;
            s.hipSpread += a.spreadAdd;
            s.adsSpread += a.spreadAdd;
            s.range     += a.rangeAdd;
            s.recoil    += a.recoilAdd;
        }

        //Multiplicative pass (after additive to get predictable scaling)
        for (int i = 0; i < attachments.Count; i++)
        {
            var a = attachments[i];
            if (a == null) continue;
            s.damage    *= a.damageMult;
            s.hipSpread *= a.spreadMult;
            s.adsSpread *= a.spreadMult;
            s.range     *= a.rangeMult;
            s.recoil    *= a.recoilMult;
            s.aimMoveSpeedMultiplier *= a.aimSpeedMult;
            if (a is ScopeAttachment scope && scope.overrideAimFOV) s.aimFOV = scope.aimFOVOverride;
        }

        return s;
    }
}
