using System;
using System.Collections.Generic;
using UnityEngine;

public class AmmoReserve : MonoBehaviour
{
    [Serializable]
    public struct StartingAmmo
    {
        public AmmoType ammo;
        public int amount;
    }

    //Seeded reserves (editor-configured) #=====#
    public List<StartingAmmo> startingAmmo = new List<StartingAmmo>();

    //Runtime
    private readonly Dictionary<AmmoType, int> counts = new Dictionary<AmmoType, int>();

    public event Action<AmmoType, int> OnReserveChanged;

    private void Awake()
    {
        for (int i = 0; i < startingAmmo.Count; i++)
        {
            var e = startingAmmo[i];
            if (e.ammo == null) continue;
            Add(e.ammo, e.amount);
        }
    }

    public int GetCount(AmmoType type)
    {
        if (type == null) return 0;
        return counts.TryGetValue(type, out int c) ? c : 0;
    }

    public void Add(AmmoType type, int amount)
    {
        if (type == null || amount <= 0) return;
        int cur = GetCount(type);
        int next = Mathf.Min(cur + amount, type.reserveCap);
        counts[type] = next;
        OnReserveChanged?.Invoke(type, next);
    }

    public int TryConsume(AmmoType type, int desired)
    {
        if (type == null || desired <= 0) return 0;
        int cur = GetCount(type);
        int taken = Mathf.Min(cur, desired);
        counts[type] = cur - taken;
        OnReserveChanged?.Invoke(type, cur - taken);
        return taken;
    }
}
