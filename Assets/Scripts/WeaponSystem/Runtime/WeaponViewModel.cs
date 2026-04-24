using UnityEngine;

// Thin animation/FX bridge between a Weapon and its visual rig.
// Kept separate so clips/Animator can be authored & swapped without touching firing logic.
public class WeaponViewModel : MonoBehaviour
{
    //Animator parameter conventions #=========#
    public const string ParamFire     = "Fire";
    public const string ParamReload   = "Reload";
    public const string ParamEquip    = "Equip";
    public const string ParamHolster  = "Holster";
    public const string ParamInspect  = "Inspect";
    public const string ParamAiming   = "Aiming";
    public const string ParamEmpty    = "Empty";
    public const string ParamFireRate = "FireRate";

    //Refs #=======================#
    public Weapon weapon;
    public Animator animator;

    //Optional VFX #===============#
    public ParticleSystem muzzleFlash;

    private void Reset()
    {
        weapon = GetComponentInParent<Weapon>();
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (weapon == null) weapon = GetComponentInParent<Weapon>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (weapon == null) return;

        weapon.OnFired         += HandleFired;
        weapon.OnReloadStart   += HandleReloadStart;
        weapon.OnReloadFinish  += HandleReloadFinish;
        weapon.OnEquip         += HandleEquip;
        weapon.OnHolster       += HandleHolster;
        weapon.OnAimIn         += HandleAimIn;
        weapon.OnAimOut        += HandleAimOut;
        weapon.OnInspect       += HandleInspect;
        weapon.OnAmmoChanged   += HandleAmmoChanged;

        //Apply per-weapon animator override if one is supplied
        if (animator != null && weapon.data != null)
        {
            if (weapon.data.animatorOverride != null)
            {
                animator.runtimeAnimatorController = weapon.data.animatorOverride;
            }
            animator.SetFloat(ParamFireRate, weapon.data.fireRate);
        }
    }

    private void OnDisable()
    {
        if (weapon == null) return;
        weapon.OnFired        -= HandleFired;
        weapon.OnReloadStart  -= HandleReloadStart;
        weapon.OnReloadFinish -= HandleReloadFinish;
        weapon.OnEquip        -= HandleEquip;
        weapon.OnHolster      -= HandleHolster;
        weapon.OnAimIn        -= HandleAimIn;
        weapon.OnAimOut       -= HandleAimOut;
        weapon.OnInspect      -= HandleInspect;
        weapon.OnAmmoChanged  -= HandleAmmoChanged;
    }

    private void HandleFired()
    {
        if (animator != null) animator.SetTrigger(ParamFire);
        if (muzzleFlash != null) muzzleFlash.Play();
    }
    private void HandleReloadStart()  { if (animator != null) animator.SetTrigger(ParamReload); }
    private void HandleReloadFinish() { /* hook for later — finalize anim, swap magazine model, etc. */ }
    private void HandleEquip()        { if (animator != null) animator.SetTrigger(ParamEquip); }
    private void HandleHolster()      { if (animator != null) animator.SetTrigger(ParamHolster); }
    private void HandleAimIn()        { if (animator != null) animator.SetBool(ParamAiming, true); }
    private void HandleAimOut()       { if (animator != null) animator.SetBool(ParamAiming, false); }
    private void HandleInspect()      { if (animator != null) animator.SetTrigger(ParamInspect); }
    private void HandleAmmoChanged(int mag, int reserve)
    {
        if (animator != null) animator.SetBool(ParamEmpty, mag <= 0);
    }
}
