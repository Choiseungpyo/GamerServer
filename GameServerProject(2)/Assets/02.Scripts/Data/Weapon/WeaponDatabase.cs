using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct WeaponStat
{
    public readonly int id;
    public readonly string name;
    public readonly int attackPower;

    public WeaponStat(int id, string name, int attackPower)
    {
        this.id = id;
        this.name = name;
        this.attackPower = attackPower;
    }
}

[Serializable]
public class WeaponDatabase
{
    [SerializeField] private WeaponVisualDatabaseSO visualDatabaseSO;
    private readonly Dictionary<int, WeaponStat> statDatabase = new Dictionary<int, WeaponStat>();

    public void Init()
    {
        if (visualDatabaseSO != null)
            visualDatabaseSO.Build();
    }

    public void SetServerStats(WeaponInfo[] informations)
    {
        statDatabase.Clear();
        if (informations == null) return;

        for (int i = 0; i < informations.Length; i++)
        {
            var s = informations[i];

            var st = new WeaponStat(
                s.weaponId,
                MarshalNet.ReadFixedAscii(s.weaponName),
                s.attackPower
            );

            statDatabase[s.weaponId] = st;
        }
    }

    public bool TryGetVisual(int weaponId, out WeaponData visual)
    {
        visual = null;
        if (visualDatabaseSO == null) return false;
        return visualDatabaseSO.TryGet(weaponId, out visual);
    }

    public bool TryGetStat(int weaponId, out WeaponStat stat)
    {
        return statDatabase.TryGetValue(weaponId, out stat);
    }
}
