using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class CharacterVisualData
{
    public int characterId;
    public GameObject modelPrefab;
}

public readonly struct CharacterStat
{
    public readonly int id;
    public readonly string name;
    public readonly int hp;
    public readonly float moveSpeed;
    public readonly int attackPower;

    public CharacterStat(int id, string name, int hp, float moveSpeed, int attackPower)
    {
        this.id = id;
        this.name = name;
        this.hp = hp;
        this.moveSpeed = moveSpeed;
        this.attackPower = attackPower;
    }
}

[Serializable]
public class CharacterDatabase
{
    [SerializeField] private CharacterVisualDatabaseSO visualDatabaseSO;

    private readonly Dictionary<int, CharacterStat> statDatabase = new Dictionary<int, CharacterStat>();

    public void Init()
    {
        if (visualDatabaseSO != null)
            visualDatabaseSO.Build();
    }

    public void SetServerStats(CharacterInfo[] informations)
    {
        statDatabase.Clear();
        if (informations == null) return;

        for (int i = 0; i < informations.Length; i++)
        {
            var s = informations[i];
            string name = MarshalNet.ReadFixedAscii(s.characterName);

            var stat = new CharacterStat(
                s.characterId,
                name,
                s.hp,
                s.moveSpeed,
                s.attackPower
            );

            statDatabase[s.characterId] = stat;
        }
    }

    public bool TryGetVisual(int characterId, out CharacterVisualData visual)
    {
        visual = null;
        if (visualDatabaseSO == null) return false;
        return visualDatabaseSO.TryGet(characterId, out visual);
    }

    public bool TryGetStat(int characterId, out CharacterStat stat)
    {
        return statDatabase.TryGetValue(characterId, out stat);
    }

    public bool TryGetFull(int characterId, out CharacterVisualData visual, out CharacterStat stat)
    {
        visual = null;
        stat = default;

        if (!TryGetVisual(characterId, out visual)) return false;
        if (!TryGetStat(characterId, out stat)) return false;
        return true;
    }
}