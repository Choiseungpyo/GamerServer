using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum TeamType
{
    RED,
    BLUE
}

public class Team
{
    #region Variables
    Dictionary<int, Player> playerDatas;
    TeamType teamType;

    int maxPeopleNum;
    int currPeopleNum;
    #endregion

    #region Properties
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

    public Team(TeamType teamType)
    {
        this.teamType = teamType;
        playerDatas = new Dictionary<int, Player>();

        maxPeopleNum = 0;
        currPeopleNum = 0;
    }

    public void AddPlayer(Player player)
    {
        playerDatas.Add(player.Id, player);
    }
}
