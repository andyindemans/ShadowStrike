using UnityEngine;

// Per-weapon sprint pose tunables. Embedded inline on WeaponData (mirrors ADSProfile).
[System.Serializable]
public struct SprintPoseProfile
{
    //Static offsets (SprintRig local space) #====#
    public Vector3 sprintPositionOffset;    // viewmodel translation while sprinting
    public Vector3 sprintRotationOffset;    // Euler degrees

    //Cyclic bob (added on top of the static offsets) #====#
    public Vector3 bobPositionAmplitude;
    public Vector3 bobRotationAmplitude;    // Euler degrees
    public float bobFrequency;              // cycles per second

    //Lerp #=================================#
    public float lerpSpeed;                 // 1/sec exponential decay rate (in and out)
}
