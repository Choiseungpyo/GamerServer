using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class EntityManager : Singleton<EntityManager>
{
    [System.Serializable]
    public struct EntityData
    {
        public int id;
        public PlayerEntity playerEntity;
    }

    [SerializeField] private List<EntityData> entityDatas;

    // <UserId, PlayerEntity>
    private Dictionary<int, PlayerEntity> playerEntityDict = new(); // RedTeam + BlueTeam

    [SerializeField] private PlayerEntityPool redTeamEntityPool;
    [SerializeField] private PlayerEntityPool blueTeamEntityPool;

    protected override void Awake()
    {
        base.Awake();

        foreach(var entityData in entityDatas)
            playerEntityDict.Add(entityData.id, entityData.playerEntity);
    }

    public void SpawnAllEntity(List<PACKET_S_C_PLAYERENTITY_DATA> packs)
    {
        foreach(var pack in packs)
        {
            if (pack.TeamType == TeamType.RED)
            {
                var playerEntity = redTeamEntityPool.Get(pack.Id);
                playerEntity.UpdataeData(pack);
            }
            else
            {
                var playerEntity = blueTeamEntityPool.Get(pack.Id);
                playerEntity.UpdataeData(pack);
            }
        }
    }

    public void UpdateEntityData(PACKET_S_C_PLAYERENTITY_DATA data)
    {
        var playerEntity = GetPlayerEntity(data.Id);

        if (!playerEntity)
            return;

        playerEntity.UpdataeData(data);
    }

    public void DeleteAllEntity()
    {
        foreach(var entity in playerEntityDict)
        {
            if(!redTeamEntityPool.Release(entity.Key))
                blueTeamEntityPool.Release(entity.Key);
        }
    }

    public PlayerEntity GetPlayerEntity(int id)
    {
        if (id < 0 || id >= playerEntityDict.Count)
            return null;

        PlayerEntity PlayerEntity = playerEntityDict[id];

        if (!PlayerEntity)
        {
            Debug.LogWarning(id);
            return null;
        }
            
        return PlayerEntity;
    }
}
