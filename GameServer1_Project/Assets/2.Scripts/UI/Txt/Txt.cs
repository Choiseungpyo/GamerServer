using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Txt : PoolableObject
{
    int userId;
    TMP_Text text;

    private void Awake()
    {
        text = transform.GetComponent<TMP_Text>();
    }

    protected override void OnSpawn()
    {

    }

    protected override void OnDespawn()
    {
        text.text = "";
    }

    public void SetText(string msg)
    {
        text.text = msg;
    }
}
