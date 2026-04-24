using UnityEngine;

public class HitscanFirer : IFirer
{
    private readonly LayerMask hitMask;
    private readonly GameObject impactEffect;

    public HitscanFirer(LayerMask hitMask, GameObject impactEffect)
    {
        this.hitMask = hitMask;
        this.impactEffect = impactEffect;
    }

    public void Fire(in FireContext ctx)
    {
        float spread = ctx.aiming ? ctx.stats.adsSpread : ctx.stats.hipSpread;
        Vector3 dir = Spread.Apply(ctx.direction, spread);

        if (Physics.Raycast(ctx.origin, dir, out RaycastHit hit, ctx.stats.range, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(ctx.stats.damage, hit.point, hit.normal, ctx.owner);
            }
            if (impactEffect != null)
            {
                Object.Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            }
            Debug.DrawLine(ctx.origin, hit.point, Color.red, 0.1f);
        }
        else
        {
            Debug.DrawRay(ctx.origin, dir * ctx.stats.range, Color.yellow, 0.1f);
        }
    }
}
