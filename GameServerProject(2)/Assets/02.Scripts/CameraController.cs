using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CameraState
{
    MainPlay,
    Spectator
}

public class CameraController : Singleton<CameraController>
{
    public void SetCameraPos(Transform parent, bool worldPositionStays)
    {
        transform.SetParent(parent, worldPositionStays);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
