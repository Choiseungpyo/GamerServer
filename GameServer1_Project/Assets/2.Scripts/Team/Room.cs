using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoomState
{
    WATING,
    PLAYING
}

public enum MatchType
{
    Solo=1,
    Duo=2,
    Squad=4
}

public class Room
{
    #region Variables
    int no;
    string roomName;

    int maxPeopleNum;
    int currPeopleNum;

    RoomState state;
    MatchType matchType;

    Dictionary<int, Player> playerDatas; // PlayerId    Player¡§∫∏
    Team[] teams; 


    #endregion

    #region Properties
    public int No
    {
        get { return no; }
        set { no = value; }
    }

    public string RoomName
    {
        get { return roomName; }
        set { roomName = value; }
    }

    public int MaxPeopleNum
    {
        get { return maxPeopleNum; }
        set { maxPeopleNum = value; }
    }

    public int CurrPeopleNum
    {
        get { return currPeopleNum; }
        set { currPeopleNum = value; }
    }
    #endregion


    public Room(MatchType matchType, int no, string roomName, int maxPeopleNum, Player player)
    {
        this.matchType = matchType;
        this.no = no;
        this.roomName = roomName;
        this.maxPeopleNum = maxPeopleNum;

        currPeopleNum = 1;
        state = RoomState.WATING;

        playerDatas = new Dictionary<int, Player>();
        playerDatas.Add(player.Id, player);
        teams = new Team[2];

        teams[0] = new Team(TeamType.RED);
        teams[1] = new Team(TeamType.BLUE);

        for (int i=0; i<teams.Length; i++)
            teams[i].MaxPeopleNum = (int)matchType;

    }

    public void SetRoomState(RoomState roomState)
    {
        state = roomState;
    }

    public RoomState GetRoomState()
    {
        return state;
    }

    public void AddPlayer(Player player)
    {
        playerDatas.Add(player.Id, player);

        Tuple<int, Team> minTeam = Tuple.Create(int.MaxValue, (Team)null);

        foreach (var team in teams)
        {
            if (team.CurrPeopleNum >= team.MaxPeopleNum)
                continue;

            if (team.CurrPeopleNum < minTeam.Item1)
                minTeam = Tuple.Create(team.CurrPeopleNum, team);
        }

        minTeam.Item2.AddPlayer(player);
    }
}
