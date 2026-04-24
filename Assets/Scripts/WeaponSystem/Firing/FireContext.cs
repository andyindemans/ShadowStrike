using UnityEngine;

public struct FireContext
{
    public Vector3 origin;
    public Vector3 direction;   // normalized
    public WeaponStats stats;
    public AmmoType ammo;
    public Transform owner;
    public bool aiming;
}
