using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Game/CharacterVisualDatabase")]
public class CharacterVisualDatabaseSO : ScriptableObject
{
    [SerializeField] private List<CharacterVisualData> list = new List<CharacterVisualData>();

    private readonly Dictionary<int, CharacterVisualData> dict = new Dictionary<int, CharacterVisualData>();

    public void Build()
    {
        dict.Clear();
        for (int i = 0; i < list.Count; i++)
        {
            var r = list[i];
            if (r == null) continue;
            if (r.modelPrefab == null) continue;
            dict[r.characterId] = r;
        }
    }

    public bool TryGet(int characterId, out CharacterVisualData data)
    {
        return dict.TryGetValue(characterId, out data);
    }

    public bool TryGetPrefab(int characterId, out GameObject prefab)
    {
        prefab = null;
        if (!TryGet(characterId, out var data)) return false;
        prefab = data.modelPrefab;
        return prefab != null;
    }
}