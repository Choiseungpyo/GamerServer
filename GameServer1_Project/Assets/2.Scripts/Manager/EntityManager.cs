using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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

    [SerializeField] private List<Transform> redTeamSpawnTr;
    [SerializeField] private List<Transform> blueTeamSpawnTr;



    protected override void Awake()
    {
        base.Awake();

        foreach(var entityData in entityDatas)
            playerEntityDict.Add(entityData.id, entityData.playerEntity);
    }

    public void SpawnAllEntity(List<PACKET_S_C_PLAYERENTITY_DATA> packs)
    {
        int hostId = TcpManager.Instance.Id;

        foreach(var pack in packs)
        {
            PlayerEntity playerEntity;
            float dir;
            if (pack.TeamType == TeamType.RED)
            {
                playerEntity = redTeamEntityPool.Get(pack.Id);
                playerEntity.UpdataeData(pack);
                dir = -0.2f;
            }
            else
            {
                playerEntity = blueTeamEntityPool.Get(pack.Id);
                playerEntity.UpdataeData(pack);
                dir = 0.2f;
            }

            if (hostId != pack.Id)
                continue;

            TcpManager.Instance.RegisterJop(() =>
            {
                Camera.main.transform.SetParent(playerEntity.transform);
                Camera.main.transform.localPosition = new Vector3(0, 1.3f, dir);
                Camera.main.transform.localRotation = playerEntity.transform.rotation;
            });

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
