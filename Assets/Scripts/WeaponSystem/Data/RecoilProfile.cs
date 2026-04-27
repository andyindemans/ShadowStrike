using UnityEngine;

// Per-weapon recoil tunables. Embedded inline on WeaponData (not a separate
// ScriptableObject — every weapon has its own profile, no asset reuse needed).
[System.Serializable]
public struct RecoilProfile
{
    //Camera kick (degrees per shot) #====#
    public float verticalKick;
    public float verticalKickVariance;
    public float horizontalKick;
    [Range(-1f, 1f)] public float horizontalBias;   // 0 = symmetric random, +1 = always right

    //Recovery #==========================#
    public float recoveryDelay;                     // seconds before recovery starts
    public float recoverySpeed;                     // 1/sec exponential decay rate
    [Range(0f, 1f)] public float recoveryFraction;  // 1 = full snap back, 0 = permanent kick

    //ADS #===============================#
    [Range(0f, 1f)] public float hipCameraRecoilMultiplier;
    [Range(0f, 1f)] public float aimCameraRecoilMultiplier;
    [Range(0f, 1f)] public float aimVisualRecoilMultiplier;

    //Visual gun kick (local space) #====#
    public Vector3 visualPositionKick;              // typically (0, 0, -0.04)
    public Vector3 visualRotationKick;              // typically (-6, 0, 0) — gun tips up
    public float visualPositionStiffness;
    public float visualRotationStiffness;
    [Range(0f, 2f)] public float visualDampingRatio;
    public float visualImpulseScale;

    // legacy — replaced by spring
    [System.Obsolete, HideInInspector] public float visualSnapSpeed;
    [System.Obsolete, HideInInspector] public float visualReturnSpeed;
}
