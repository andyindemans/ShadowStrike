using UnityEngine;

// Per-weapon procedural motion tunables. Embedded inline on WeaponData (mirrors ADSProfile/SprintPoseProfile).
// Drives WeaponMotion's three composable layers: walk bob, idle breathing, look-delay spring sway.
[System.Serializable]
public struct WeaponMotionProfile
{
    //Walk Bob #=================#
    public Vector3 walkBobPosAmplitude;
    public Vector3 walkBobRotAmplitude;     // Euler degrees
    public float walkBobBaseFrequency;
    public float walkBobMinSpeed;
    public float walkBobNoiseFrequency;
    public float walkBobVariance;           // Perlin amplitude modulator strength (0..1).
    public float walkBobSmoothFrequency;    // Output spring's natural frequency in Hz.
    public float walkBobSmoothDamping;      // Output spring damping ratio.

    //Idle Breathing #===========#
    public float idleBreathFrequency;
    public Vector3 idleBreathAmplitude;
    public Vector3 idleBreathRotAmplitude;  // Euler degrees
    public float idleBreathADSScale;
    public float idleBreathMoveFadeSpeed;

    //Look Sway Spring #=========#
    public Vector3 lookSwayMaxOffset;
    public Vector3 lookSwayMaxRot;          // Euler degrees
    public Vector2 lookSwaySensitivity;
    public float lookSwayStiffness;
    public float lookSwayDampingRatio;

    // Struct field initializers don't run for `default(T)` / Unity-deserialized zeros, so this
    // explicit constructor is the single source of authored defaults — referenced by WeaponData's
    // field initializer and by WeaponMotion's fallback when no weapon is active.
    public static WeaponMotionProfile Default => new WeaponMotionProfile
    {
        walkBobPosAmplitude = new Vector3(0.012f, 0.018f, 0.006f),
        walkBobRotAmplitude = new Vector3(0.5f, 0.3f, 0.6f),
        walkBobBaseFrequency = 1f,
        walkBobMinSpeed = 0.5f,
        walkBobNoiseFrequency = 0.7f,
        walkBobVariance = 0.5f,
        walkBobSmoothFrequency = 10f,
        walkBobSmoothDamping = 1f,

        idleBreathFrequency = 0.6f,
        idleBreathAmplitude = new Vector3(0f, 0.0015f, 0f),
        idleBreathRotAmplitude = new Vector3(0.25f, 0.15f, 0f),
        idleBreathADSScale = 0.35f,
        idleBreathMoveFadeSpeed = 8f,

        lookSwayMaxOffset = new Vector3(0.018f, 0.012f, 0f),
        lookSwayMaxRot = new Vector3(2.5f, 4f, 1.5f),
        lookSwaySensitivity = new Vector2(0.0025f, 0.0025f),
        lookSwayStiffness = 120f,
        lookSwayDampingRatio = 0.85f,
    };
}
