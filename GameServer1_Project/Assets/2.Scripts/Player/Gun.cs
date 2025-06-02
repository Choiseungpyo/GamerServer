using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    private const int maxBulletCnt = 30;
    private int currBulletCnt;

    public int CurrBulletCnt
    {
        get { return currBulletCnt; }
        set { currBulletCnt = value; }
    }

    // Start is called before the first frame update
    void Start()
    {
        Reload();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Reload()
    {
        currBulletCnt = maxBulletCnt;
    }
}
