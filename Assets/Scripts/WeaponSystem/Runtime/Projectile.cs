using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    //Config #=====================#
    public float lifetime = 5f;
    public GameObject impactEffect;

    //Runtime #====================#
    private Rigidbody rb;
    private float damage;
    private Transform owner;
    private bool launched;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 velocity, float damage, float gravityMultiplier, Transform owner)
    {
        this.damage = damage;
        this.owner = owner;

        rb.useGravity = gravityMultiplier > 0f;
        rb.linearVelocity = velocity;
        launched = true;

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!launched) return;
        if (owner != null && collision.collider.transform.IsChildOf(owner)) return;
        if (collision.collider.TryGetComponent<Projectile>(out _)) return; // ignore other in-flight projectiles (e.g. shotgun pellets spawning together)

        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        Vector3 normal = collision.contactCount > 0 ? collision.GetContact(0).normal : -transform.forward;

        if (collision.collider.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.TakeDamage(damage, point, normal, owner);
        }
        if (impactEffect != null)
        {
            Instantiate(impactEffect, point, Quaternion.LookRotation(normal));
        }
        Destroy(gameObject);
    }
}
