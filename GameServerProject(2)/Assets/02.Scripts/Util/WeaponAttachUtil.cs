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

    public static Transform GetMuzzle(Transform weaponRoot, string muzzleName)
    {
        return FindChildRecursive(weaponRoot, muzzleName);
    }
}