using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class IconRow
{
    public int iconId;
    public Sprite sprite;
}

[CreateAssetMenu(menuName = "Game/IconVisualDatabase")]
public class IconVisualDatabaseSO : ScriptableObject
{
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private List<IconRow> list = new List<IconRow>();

    private readonly Dictionary<int, Sprite> byId = new Dictionary<int, Sprite>();

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
            if (r.sprite == null) continue;

            byId[r.iconId] = r.sprite;
        }
    }

    public Sprite GetOrDefault(int iconId)
    {
        if (byId.TryGetValue(iconId, out var s) && s != null)
            return s;

        return defaultSprite;
    }

    public bool TryGet(int iconId, out Sprite sprite)
    {
        return byId.TryGetValue(iconId, out sprite);
    }
}