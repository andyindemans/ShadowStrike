using UnityEngine;

public class ProjectileFirer : IFirer
{
    public void Fire(in FireContext ctx)
    {
        if (ctx.ammo == null || ctx.ammo.projectilePrefab == null) return;

        float spread = ctx.aiming ? ctx.stats.adsSpread : ctx.stats.hipSpread;
        int pellets = Mathf.Max(1, ctx.ammo.pelletsPerShot);

        for (int i = 0; i < pellets; i++)
        {
            Vector3 dir = Spread.Apply(ctx.direction, spread);

            Projectile p = Object.Instantiate(
                ctx.ammo.projectilePrefab,
                ctx.origin,
                Quaternion.LookRotation(dir));

            p.Launch(
                dir * ctx.ammo.muzzleVelocity,
                ctx.stats.damage,
                ctx.ammo.gravityMultiplier,
                ctx.owner);
        }
    }
}
