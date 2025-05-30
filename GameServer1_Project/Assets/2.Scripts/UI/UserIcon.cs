using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

public enum IconType
{
    Cat,
    Giraffe,
    Rabbit,
    Pig,
    Elephant,
    Panda,
    Monkey,
    Goat,
    Sheep,
    Raccoon,
    Lemur,
    Meerkat,
    Hippopotamus,
    Mouse,
    Dog,
    Bear,
    Lion,
    Tiger,
    Fox,
    Wolf
}

public class UserIcon : Singleton<UserIcon>
{
    Dictionary<IconType, Sprite> icons;

    protected override void Awake()
    {
        base.Awake();
        LoadAllAnimalIcons();
    }

    private void LoadAllAnimalIcons()
    {
        icons = new Dictionary<IconType, Sprite>();

        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/animalIcons");

        int i = 0;
        foreach(var iconType in System.Enum.GetValues(typeof(IconType)))
            icons[(IconType)iconType] = sprites[i++];
    }

    public Sprite GetSprite(string iconType)
    {
        if (!System.Enum.TryParse(iconType, out IconType type))
        {
            Debug.LogWarning(iconType);
            return null;
        }

        return icons.TryGetValue(type, out var sprite) ? sprite : null;
    }

    
}
