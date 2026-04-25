using UnityEngine;

// Per-weapon ADS pose tunables. Embedded inline on WeaponData (mirrors RecoilProfile —
// every weapon has its own pose, no asset reuse needed).
[System.Serializable]
public struct ADSProfile
{
    //Pose offsets (AimRig local space) #====#
    public Vector3 aimPositionOffset;       // viewmodel translation when aimed
    public Vector3 aimRotationOffset;       // Euler degrees

    //Lerp #=================================#
    public float adsLerpSpeed;              // 1/sec exponential decay rate (in and out)
}
