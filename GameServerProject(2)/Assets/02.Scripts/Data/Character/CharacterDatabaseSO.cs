using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterRuntimeData
{
    public int id;
    public string name;
    public int hp;
    public float moveSpeed;
    public int attackPower;
}

[CreateAssetMenu(menuName = "Game/Character Stat Database")]
public class CharacterDatabaseSO : ScriptableObject
{
    [SerializeField] private List<CharacterRuntimeData> list = new List<CharacterRuntimeData>();
    private readonly Dictionary<int, CharacterRuntimeData> byId = new Dictionary<int, CharacterRuntimeData>();

    public IReadOnlyList<CharacterRuntimeData> List => list;

    public void BuildFromCharacterList(CharacterListPacket pkt)
    {
        list.Clear();
        byId.Clear();

        int cnt = pkt.characterCount;
        if (cnt < 0) cnt = 0;
        if (cnt > NetConst.MAX_CHARACTERS) cnt = NetConst.MAX_CHARACTERS;

        if (pkt.characters == null) return;

        for (int i = 0; i < cnt; i++)
        {
            CharacterRow r = pkt.characters[i];

            CharacterRuntimeData d = new CharacterRuntimeData();
            d.id = r.characterId;

            if (r.characterName != null)
                d.name = MarshalNet.ReadFixedAscii(r.characterName);
            else
                d.name = "";

            d.hp = r.hp;
            d.moveSpeed = r.moveSpeed;
            d.attackPower = r.attackPower;

            list.Add(d);
            byId[d.id] = d;
        }
    }

    public bool TryGet(int characterId, out CharacterRuntimeData data)
    {
        return byId.TryGetValue(characterId, out data);
    }
}