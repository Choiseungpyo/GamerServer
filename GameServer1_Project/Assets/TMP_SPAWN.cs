using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TMP_SPAWN : MonoBehaviour
{
    private void Start()
    {
        Debug.Log(gameObject.name);
        for (int i = 0; i < transform.childCount; i++)
            Debug.Log(transform.GetChild(i).transform.position);
    }
}
