using UnityEngine;

// Minimal damage contract. Enemies / destructibles implement this later;
// hitscan & projectile firers don't care about the concrete type.
public interface IDamageable
{
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, Transform source);
}
