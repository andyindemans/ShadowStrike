using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    //Refs #=======================#
    public Movement movement;
    public PlayerInventory inventory;
    public Camera viewCamera;

    //Behaviour #==================#
    public bool firingWhileSprintingAllowed = false;
    public float aimFOVLerpSpeed = 12f;

    //Runtime
    private MovementSystem weaponInput;
    private bool firingHeld;
    private bool aimHeld;

    //Aim FOV / move-speed restore
    private float baseFOV = 60f;
    private float targetFOV = 60f;
    private float baseMoveSpeed;
    private bool movementSpeedCached;

    private void Awake()
    {
        if (movement == null) movement = GetComponentInParent<Movement>();
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        if (viewCamera == null && movement != null) viewCamera = movement.camera;

        weaponInput = new MovementSystem();
        weaponInput.Weapon.Enable();

        weaponInput.Weapon.Shoot.performed     += OnShootPerformed;
        weaponInput.Weapon.Shoot.canceled      += OnShootCanceled;
        weaponInput.Weapon.Reload.performed    += OnReload;
        weaponInput.Weapon.Aim.performed       += OnAimPerformed;
        weaponInput.Weapon.Aim.canceled        += OnAimCanceled;
        weaponInput.Weapon.SwitchWeapon.performed += OnSwitchWeapon;
        weaponInput.Weapon.Inspect.performed   += OnInspect;

        if (inventory != null) inventory.OnWeaponChanged += HandleWeaponChanged;
    }

    private void Start()
    {
        if (viewCamera != null)
        {
            baseFOV = viewCamera.fieldOfView;
            targetFOV = baseFOV;
        }
    }

    private void OnEnable()
    {
        if (weaponInput != null) weaponInput.Weapon.Enable();
    }

    private void OnDisable()
    {
        if (weaponInput != null) weaponInput.Weapon.Disable();
        ApplyAimOut();
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.OnWeaponChanged -= HandleWeaponChanged;
        if (weaponInput != null) weaponInput.Dispose();
    }

    private void Update()
    {
        //Auto-fire while held (single-shot semantics gated inside Weapon by fireRate)
        if (firingHeld) TryShootOnce();

        //Smooth FOV toward target
        if (viewCamera != null && !Mathf.Approximately(viewCamera.fieldOfView, targetFOV))
        {
            viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, targetFOV, Time.deltaTime * aimFOVLerpSpeed);
        }
    }

    //Input handlers #=============#
    private void OnShootPerformed(InputAction.CallbackContext ctx) { firingHeld = true; }
    private void OnShootCanceled(InputAction.CallbackContext ctx)  { firingHeld = false; }

    private void OnReload(InputAction.CallbackContext ctx)
    {
        var w = inventory != null ? inventory.Active : null;
        if (w != null) w.StartReload();
    }

    private void OnAimPerformed(InputAction.CallbackContext ctx) { aimHeld = true;  ApplyAimIn(); }
    private void OnAimCanceled(InputAction.CallbackContext ctx)  { aimHeld = false; ApplyAimOut(); }

    private void OnSwitchWeapon(InputAction.CallbackContext ctx)
    {
        if (inventory == null) return;
        float v = ctx.ReadValue<float>();
        if (v < 0f) inventory.Equip(0);
        else if (v > 0f) inventory.Equip(1);
    }

    private void OnInspect(InputAction.CallbackContext ctx)
    {
        var w = inventory != null ? inventory.Active : null;
        if (w != null) w.TriggerInspect();
    }

    //Actions #====================#
    private void TryShootOnce()
    {
        var w = inventory != null ? inventory.Active : null;
        if (w == null) return;
        if (movement != null && movement.isSprinting && !firingWhileSprintingAllowed) return;

        Vector3 origin = viewCamera != null ? viewCamera.transform.position : transform.position;
        Vector3 dir = viewCamera != null ? viewCamera.transform.forward : transform.forward;
        w.TryFire(origin, dir);
    }

    private void ApplyAimIn()
    {
        var w = inventory != null ? inventory.Active : null;
        if (w == null) return;

        w.StartAim();

        if (viewCamera != null && w.EffectiveStats.aimFOV > 0f)
        {
            targetFOV = w.EffectiveStats.aimFOV;
        }
        if (movement != null)
        {
            if (!movementSpeedCached)
            {
                baseMoveSpeed = movement.maxSpeed;
                movementSpeedCached = true;
            }
            movement.maxSpeed = baseMoveSpeed * Mathf.Max(0.01f, w.EffectiveStats.aimMoveSpeedMultiplier);
        }
    }

    private void ApplyAimOut()
    {
        var w = inventory != null ? inventory.Active : null;
        if (w != null) w.StopAim();

        targetFOV = baseFOV;

        if (movement != null && movementSpeedCached)
        {
            movement.maxSpeed = baseMoveSpeed;
            movementSpeedCached = false;
        }
    }

    private void HandleWeaponChanged(Weapon oldWeapon, Weapon newWeapon)
    {
        //If aim is held through a weapon swap, re-apply to the new weapon
        if (aimHeld) ApplyAimIn();
        else ApplyAimOut();
    }
}
