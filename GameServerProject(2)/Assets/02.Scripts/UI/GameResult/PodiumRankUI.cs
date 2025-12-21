using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[Serializable]
public class PodiumRankUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nicknameText;

    public void Set(Sprite icon, string nickname)
    {
        if (iconImage != null) iconImage.sprite = icon;
        if (nicknameText != null) nicknameText.text = nickname;
    }
}
