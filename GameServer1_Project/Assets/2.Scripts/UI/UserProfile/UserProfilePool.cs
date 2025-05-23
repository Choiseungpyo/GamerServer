using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserProfilePool : ObjectPoolBase<UserProfile>
{
    protected override void Awake()
    {
        //maxCount = EntityManager.Instance.MaxPlayerNum;
        base.Awake();
        
    }
}
