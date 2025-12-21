using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterVisualRow
{
    public int characterId;
    public GameObject modelPrefab;

    [Header("Default Weapon")]
    public int defaultWeaponId;
}

[CreateAssetMenu(menuName = "Game/CharacterVisualDatabase")]
public class CharacterVisualDatabaseSO : ScriptableObject
{
    [SerializeField] private List<CharacterVisualRow> list = new List<CharacterVisualRow>();

    private readonly Dictionary<int, CharacterVisualRow> byId = new Dictionary<int, CharacterVisualRow>();

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

    public void Build()
    {
        byId.Clear();
        for (int i = 0; i < list.Count; i++)
        {
            var r = list[i];
            if (r == null) continue;
            if (r.modelPrefab == null) continue;
            byId[r.characterId] = r;
        }
    }

    public bool TryGet(int characterId, out CharacterVisualRow row)
    {
        return byId.TryGetValue(characterId, out row);
    }

    public bool TryGetPrefab(int characterId, out GameObject prefab)
    {
        prefab = null;
        if (!TryGet(characterId, out var row)) return false;
        prefab = row.modelPrefab;
        return prefab != null;
    }
}