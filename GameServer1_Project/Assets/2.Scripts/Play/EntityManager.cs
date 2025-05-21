using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager : Singleton<EntityManager>, IListener
{
    [SerializeField] GameObject[] playerPrefabs;

    List<Player> players = new List<Player>();


    private void Start()
    {
        EventManager.Instance.AddListener(EVENT_TYPE.ADD_PLAYER, this);
        EventManager.Instance.AddListener(EVENT_TYPE.APPLY_PLAYER_MOVEMENT, this);
    }

    //public void AddPlayer(PACKET_S_PLAYER_DATA playerData)
    //{
    //    GameObject player = Instantiate(
    //        playerPrefabs[playerData.characterType],
    //        new Vector3(playerData.Rotation.X, playerData.Rotation.Y, playerData.Rotation.Z), 
    //        Quaternion.Euler(new Vector3(playerData.Rotation.X, playerData.Rotation.Y, playerData.Rotation.Z))
    //        , transform);
    //}

    public void AddPlayer(int id, Vector3 pos)
    {
        GameObject playerObj = Instantiate(
            playerPrefabs[0],
            pos,
            Quaternion.identity
            , transform);
        Player player = playerObj.GetComponent<Player>();
        player.Id = id;
        player.name = "Player_" + id.ToString();

        players.Add(player);
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
