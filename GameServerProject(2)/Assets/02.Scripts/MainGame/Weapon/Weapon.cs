using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponViewMode
{
    World,
    FirstPerson
}

public enum WeaponType
{
    Rifle_Default = 1,
    Rifle_Dessert = 2,
    Rifle_Forest = 3
}

public class Weapon : MonoBehaviour
{
    public Transform Muzzle { get; private set; }

    public void CacheMuzzle(string muzzleName = "Muzzle")
    {
        if (string.IsNullOrEmpty(muzzleName))
        {
            Muzzle = null;
            return;
        }

        var t = transform.Find(muzzleName);
        if (t == null) t = FindDeepChild(transform, muzzleName);
        Muzzle = t;
    }

    private Transform FindDeepChild(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c.name == name) return c;

            var r = FindDeepChild(c, name);
            if (r != null) return r;
        }
        return null;
    }
}
