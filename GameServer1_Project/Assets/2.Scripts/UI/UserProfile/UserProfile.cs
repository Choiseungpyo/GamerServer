using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserProfile : PoolableObject
{
    Image iconImg;
    TMP_Text userNameTxt;

    protected override void OnSpawn()
    {

    }

    protected override void OnDespawn()
    {
        userNameTxt.text = "";
        iconImg.sprite = null;
    }



    private void Awake()
    {
        iconImg = transform.GetChild(0).GetComponent<Image>();
        userNameTxt = transform.GetChild(1).GetComponent<TMP_Text>();
    }

    public void SetUserProfile(string userNametxt)
    {
        userNameTxt.text = userNametxt;
        SetIcon();
    }

    private void SetIcon()
    {
        Sprite iconSprite = UserIcon.Instance.GetSprite(userNameTxt.text);
        iconImg.sprite = iconSprite;
    }

}
