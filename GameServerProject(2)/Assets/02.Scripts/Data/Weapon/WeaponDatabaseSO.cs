using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponRow
{
    public int weaponId;

    [Header("World (3rd person / preview)")]
    public GameObject worldPrefab;
    public Vector3 worldLocalPos = Vector3.zero;
    public Vector3 worldLocalEuler = Vector3.zero;

    [Header("First Person")]
    public GameObject fpPrefab;
    public Vector3 fpLocalPos = new Vector3(0.25f, -0.25f, 0.6f);
    public Vector3 fpLocalEuler = Vector3.zero;

    [Header("Attach / Find")]
    public string muzzleName = "Muzzle";
}

public struct WeaponNetStat
{
    public string name;
    public int attackPower;
}

[CreateAssetMenu(menuName = "Game/WeaponVisualDatabase")]
public class WeaponDatabaseSO : ScriptableObject
{
    [SerializeField] private List<WeaponRow> list = new List<WeaponRow>();
    private readonly Dictionary<int, WeaponRow> byId = new Dictionary<int, WeaponRow>();

    public void Build()
    {
        byId.Clear();

        for (int i = 0; i < list.Count; i++)
        {
            var r = list[i];
            if (r == null) continue;

            byId[r.weaponId] = r;
        }
    }

    private void OnEnable()
    {
        Build();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Build();
    }
#endif

    public bool TryGet(int weaponId, out WeaponRow row)
    {
        return byId.TryGetValue(weaponId, out row);
    }
}