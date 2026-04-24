using System;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    //Config #=====================#
    public WeaponData data;
    public AmmoType currentAmmo;
    public Transform muzzle;           // spawn point for projectiles / VFX origin; falls back to caller origin
    public LayerMask hitMask = ~0;     // used by hitscan firer
    public GameObject impactEffect;

    //State #======================#
    public int magCount;
    public List<AttachmentData> attachments = new List<AttachmentData>();

    //Events (animation + UI hooks) #==========#
    public event Action OnFired;
    public event Action OnReloadStart;
    public event Action OnReloadFinish;
    public event Action OnEquip;
    public event Action OnHolster;
    public event Action OnAimIn;
    public event Action OnAimOut;
    public event Action OnInspect;
    public event Action<int, int> OnAmmoChanged;      // (mag, reserve)
    public event Action<AttachmentData> OnAttachmentChanged;

    //Runtime
    private WeaponStats effectiveStats;
    private IFirer firer;
    private float nextFireTime;
    private AmmoReserve reserve;
    private bool initialized;

    public WeaponStats EffectiveStats => effectiveStats;
    public bool IsReloading { get; private set; }
    public bool IsAiming { get; private set; }

    public void Initialize(AmmoReserve ammoReserve)
    {
        reserve = ammoReserve;
        if (data != null)
        {
            if (currentAmmo == null) currentAmmo = data.defaultAmmo;
            if (magCount == 0) magCount = data.magSize;
        }
        RebuildStats();
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized) return;
        RebuildStats();
        OnEquip?.Invoke();
        RaiseAmmoChanged();
    }

    private void OnDisable()
    {
        if (!initialized) return;
        if (IsAiming)
        {
            IsAiming = false;
            OnAimOut?.Invoke();
        }
        if (IsReloading)
        {
            CancelInvoke(nameof(FinishReload));
            IsReloading = false;
        }
        OnHolster?.Invoke();
    }

    public void RebuildStats()
    {
        if (data == null) return;
        effectiveStats = WeaponStats.Resolve(data, attachments, currentAmmo);
        firer = CreateFirer();
    }

    private IFirer CreateFirer()
    {
        switch (data.firingMode)
        {
            case FiringMode.Projectile: return new ProjectileFirer();
            case FiringMode.Hitscan:
            default: return new HitscanFirer(hitMask, impactEffect);
        }
    }

    public bool TryFire(Vector3 origin, Vector3 direction)
    {
        if (data == null || currentAmmo == null) return false;
        if (IsReloading) return false;
        if (Time.time < nextFireTime) return false;
        if (magCount <= 0) return false;

        var ctx = new FireContext
        {
            origin = muzzle != null ? muzzle.position : origin,
            direction = direction.normalized,
            stats = effectiveStats,
            ammo = currentAmmo,
            owner = transform,
            aiming = IsAiming,
        };
        firer.Fire(ctx);

        magCount--;
        nextFireTime = Time.time + 1f / Mathf.Max(0.01f, effectiveStats.fireRate);
        OnFired?.Invoke();
        RaiseAmmoChanged();
        return true;
    }

    public bool StartReload()
    {
        if (IsReloading) return false;
        if (data == null || currentAmmo == null) return false;
        if (magCount >= data.magSize) return false;
        if (reserve == null || reserve.GetCount(currentAmmo) <= 0) return false;

        IsReloading = true;
        OnReloadStart?.Invoke();
        Invoke(nameof(FinishReload), data.reloadDuration);
        return true;
    }

    private void FinishReload()
    {
        IsReloading = false;
        int missing = data.magSize - magCount;
        int taken = reserve.TryConsume(currentAmmo, missing);
        magCount += taken;
        OnReloadFinish?.Invoke();
        RaiseAmmoChanged();
    }

    public void StartAim()
    {
        if (IsAiming) return;
        IsAiming = true;
        OnAimIn?.Invoke();
    }

    public void StopAim()
    {
        if (!IsAiming) return;
        IsAiming = false;
        OnAimOut?.Invoke();
    }

    public void TriggerInspect()
    {
        OnInspect?.Invoke();
    }

    //Attachments #================#
    public bool CanAccept(AttachmentData attachment)
    {
        if (attachment == null || data == null) return false;
        return (data.allowedAttachmentSlots & attachment.slot) == attachment.slot;
    }

    public void AddAttachment(AttachmentData attachment)
    {
        if (!CanAccept(attachment)) return;
        // Replace any attachment already in this slot
        for (int i = attachments.Count - 1; i >= 0; i--)
        {
            if (attachments[i] != null && attachments[i].slot == attachment.slot)
            {
                attachments.RemoveAt(i);
            }
        }
        attachments.Add(attachment);
        RebuildStats();
        OnAttachmentChanged?.Invoke(attachment);
    }

    public void RemoveAttachment(AttachmentData attachment)
    {
        if (attachments.Remove(attachment))
        {
            RebuildStats();
            OnAttachmentChanged?.Invoke(attachment);
        }
    }

    private void RaiseAmmoChanged()
    {
        int reserveCount = (reserve != null && currentAmmo != null) ? reserve.GetCount(currentAmmo) : 0;
        OnAmmoChanged?.Invoke(magCount, reserveCount);
    }
}
