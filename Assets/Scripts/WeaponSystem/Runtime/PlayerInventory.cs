using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public const int SLOT_COUNT = 2;

    //Slots #======================#
    public Weapon[] slots = new Weapon[SLOT_COUNT];
    public int activeSlot = 0;

    //Ammo #=======================#
    public AmmoReserve reserve;

    //Events #=====================#
    public event Action<Weapon, Weapon> OnWeaponChanged; // (oldActive, newActive)

    public Weapon Active =>
        (slots != null && activeSlot >= 0 && activeSlot < slots.Length) ? slots[activeSlot] : null;

    private void Awake()
    {
        if (reserve == null) reserve = GetComponent<AmmoReserve>();
        if (slots == null || slots.Length != SLOT_COUNT)
        {
            var resized = new Weapon[SLOT_COUNT];
            if (slots != null)
            {
                for (int i = 0; i < Mathf.Min(slots.Length, SLOT_COUNT); i++) resized[i] = slots[i];
            }
            slots = resized;
        }
        activeSlot = Mathf.Clamp(activeSlot, 0, slots.Length - 1);
    }

    private void Start()
    {
        //Disable all first so Initialize can set state before OnEnable fires events.
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) slots[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < slots.Length; i++)
        {
            var w = slots[i];
            if (w == null) continue;
            w.Initialize(reserve);
        }
        //Now enable the active slot — this fires OnEnable → OnEquip / OnAmmoChanged cleanly.
        if (Active != null) Active.gameObject.SetActive(true);
    }

    public void Equip(int slot)
    {
        if (slots == null || slot < 0 || slot >= slots.Length) return;
        if (slot == activeSlot && slots[slot] != null && slots[slot].gameObject.activeSelf) return;

        var oldActive = Active;
        if (oldActive != null) oldActive.gameObject.SetActive(false);

        activeSlot = slot;
        var newActive = Active;
        if (newActive != null) newActive.gameObject.SetActive(true);

        OnWeaponChanged?.Invoke(oldActive, newActive);
    }

    public void Cycle(int direction)
    {
        if (direction == 0 || slots == null || slots.Length == 0) return;
        int step = direction > 0 ? 1 : -1;
        for (int i = 1; i <= slots.Length; i++)
        {
            int idx = ((activeSlot + step * i) % slots.Length + slots.Length) % slots.Length;
            if (slots[idx] != null) { Equip(idx); return; }
        }
    }

    public bool PickUp(Weapon weapon, int preferredSlot = -1)
    {
        if (weapon == null) return false;
        int target = preferredSlot >= 0 && preferredSlot < slots.Length ? preferredSlot : FindEmptySlot();
        if (target < 0) target = activeSlot; // replace active if full
        if (slots[target] != null) Destroy(slots[target].gameObject);
        slots[target] = weapon;
        weapon.gameObject.SetActive(false);       // initialize before any OnEnable event fires
        weapon.Initialize(reserve);
        weapon.gameObject.SetActive(target == activeSlot);
        return true;
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++) if (slots[i] == null) return i;
        return -1;
    }
}
