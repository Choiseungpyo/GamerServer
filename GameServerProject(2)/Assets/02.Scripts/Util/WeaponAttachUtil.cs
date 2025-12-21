using UnityEngine;

public static class WeaponAttachUtil
{
    public static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null) return null;
        if (root.name == targetName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null) return found;
        }
        return null;
    }

    public static Transform GetRightHand(Transform modelRoot, string rightHandName = "hand_r")
    {
        var animator = modelRoot.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            var t = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (t != null) return t;
        }

        return FindChildRecursive(modelRoot, rightHandName);
    }

    public static GameObject AttachWorldWeapon(Transform modelRoot, GameObject weaponPrefab, Vector3 localPos, Vector3 localEuler, string rightHandName = "hand_r")
    {
        var hand = GetRightHand(modelRoot, rightHandName);
        if (hand == null || weaponPrefab == null) return null;

        var weapon = Object.Instantiate(weaponPrefab, hand);
        weapon.transform.localPosition = localPos;
        weapon.transform.localRotation = Quaternion.Euler(localEuler);
        return weapon;
    }

    public static GameObject AttachFpWeapon(Transform fpRoot, GameObject weaponPrefab, Vector3 localPos, Vector3 localEuler)
    {
        if (fpRoot == null || weaponPrefab == null) return null;

        var weapon = Object.Instantiate(weaponPrefab, fpRoot);
        weapon.transform.localPosition = localPos;
        weapon.transform.localRotation = Quaternion.Euler(localEuler);
        return weapon;
    }

    public static Transform GetMuzzle(Transform weaponRoot, string muzzleName)
    {
        return FindChildRecursive(weaponRoot, muzzleName);
    }
}