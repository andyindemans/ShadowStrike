using UnityEngine;

[CreateAssetMenu(fileName = "AmmoType", menuName = "ShadowStrike/Weapons/Ammo Type")]
public class AmmoType : ScriptableObject
{
    //Identity #===================#
    public string id;
    public string displayName;

    //Ballistics #=================#
    public float baseDamage = 10f;
    public float muzzleVelocity = 80f;     // m/s — used by ProjectileFirer
    public float gravityMultiplier = 1f;   // 0 = straight line
    public Projectile projectilePrefab;    // null ⇒ hitscan-only

    //Reserve #====================#
    public int reserveCap = 120;
}
