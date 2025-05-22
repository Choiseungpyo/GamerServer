using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager : Singleton<EntityManager>, IListener
{
    [SerializeField] GameObject[] player_Prefabs;

    Dictionary<int, Player> players;
    int currPlyerId;

    private void Start()
    {
        players = new Dictionary<int, Player>();
        EventManager.Instance.AddListener(EVENT_TYPE.ADD_PLAYER, this);
        EventManager.Instance.AddListener(EVENT_TYPE.APPLY_PLAYER_MOVEMENT, this);

        currPlyerId = TcpManager.Instance.Id;

        GameObject playerObj = Instantiate(player_Prefabs[0]);

        Player player = playerObj.GetComponent<Player>();
        player.Id = 0;
        EntityManager.Instance.AddPlayer(player.Id, Vector3.zero);
    }

    //public void AddPlayer(PACKET_S_PLAYER_DATA playerData)
    //{
    //    GameObject player = Instantiate(
    //        player_Prefabs[playerData.characterType],
    //        new Vector3(playerData.Rotation.X, playerData.Rotation.Y, playerData.Rotation.Z), 
    //        Quaternion.Euler(new Vector3(playerData.Rotation.X, playerData.Rotation.Y, playerData.Rotation.Z))
    //        , transform);
    //}

    public void AddPlayer(int id, Vector3 pos)
    {
        GameObject playerObj = Instantiate(
            player_Prefabs[0],
            pos,
            Quaternion.identity
            , transform);
        Player player = playerObj.GetComponent<Player>();
        player.Id = id;
        player.name = "Player_" + id.ToString();

        players.Add(id, player);
    }

    public Player GetPlayer(int id)
    {
        if (id < 0 || id >= players.Count)
            return null;

        Player player = players[id];

        if (!player)
            Debug.LogWarning(id);
        return players[id];
    }

    public bool IsCurrPlayer(int id)
    {
        return currPlyerId == id;
    }

    private void SetPlayerPos(int id, Vector3 pos)
    {
        if(id < 0 || players.Count <= id)
        {
            Debug.LogError($"id : {id}");
            return;
        }
        players[id].transform.position = pos;
    }

    public void OnEvent(EVENT_TYPE EventType, Component Sender, object Param = null)
    {
        Vector3 tmp_pos;

        switch (EventType)
        {
            case EVENT_TYPE.ADD_PLAYER:
                {
                    PACKET_S_SPAWN packet = (PACKET_S_SPAWN)Param;
                    TcpManager.Instance.RegisterJop(() =>
                    {
                        tmp_pos.x = packet.Position.X;
                        tmp_pos.y = packet.Position.Y;
                        tmp_pos.z = packet.Position.Z;
                        AddPlayer(packet.Id, tmp_pos);
                    });
                }
                break;

            case EVENT_TYPE.APPLY_PLAYER_MOVEMENT:
                {
                    PACKET_S_MOVE packet = (PACKET_S_MOVE)Param;
                    TcpManager.Instance.RegisterJop(() =>
                    {
                        tmp_pos.x = packet.Position.X;
                        tmp_pos.y = packet.Position.Y;
                        tmp_pos.z = packet.Position.Z;
                        SetPlayerPos(packet.Id, tmp_pos);
                    });
                }
                break;
        }
    }

}
