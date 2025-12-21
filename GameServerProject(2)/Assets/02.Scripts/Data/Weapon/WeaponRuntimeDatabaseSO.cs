using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponRuntimeData
{
    public int id;
    public string name;
    public int attackPower;
}

[CreateAssetMenu(menuName = "Game/WeaponRuntimeDatabase")]
public class WeaponRuntimeDatabaseSO : ScriptableObject
{
    [SerializeField] private List<WeaponRuntimeData> list = new List<WeaponRuntimeData>();

    [NonSerialized] private Dictionary<int, WeaponRuntimeData> byId;

    public IReadOnlyList<WeaponRuntimeData> List => list;

    private void OnEnable()
    {
        EnsureInit();
        RebuildIndexFromList();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureInit();
        RebuildIndexFromList();
    }
#endif

    private void EnsureInit()
    {
        if (list == null) list = new List<WeaponRuntimeData>();
        if (byId == null) byId = new Dictionary<int, WeaponRuntimeData>();
    }

    private void RebuildIndexFromList()
    {
        byId.Clear();

        for (int i = 0; i < list.Count; i++)
        {
            var d = list[i];
            if (d == null) continue;
            byId[d.id] = d;
        }
    }

    public void BuildFromWeaponList(WeaponListPacket pkt)
    {
        EnsureInit();

        list.Clear();
        byId.Clear();

        int cnt = pkt.weaponCount;
        if (cnt < 0) cnt = 0;
        if (cnt > NetConst.MAX_WEAPONS) cnt = NetConst.MAX_WEAPONS;

        if (pkt.weapons == null || pkt.weapons.Length < cnt)
            return;

        for (int i = 0; i < cnt; i++)
        {
            WeaponInfo w = pkt.weapons[i];

            WeaponRuntimeData d = new WeaponRuntimeData();
            d.id = w.weaponId;
            d.name = MarshalNet.ReadFixedAscii(w.weaponName);
            d.attackPower = w.attackPower;

            list.Add(d);
            byId[d.id] = d;
        }
    }

    public bool TryGet(int weaponId, out WeaponRuntimeData data)
    {
        EnsureInit();
        return byId.TryGetValue(weaponId, out data);
    }
}