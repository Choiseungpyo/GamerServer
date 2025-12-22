using UnityEngine;

public enum CharacterType
{
    Male = 1,
    Female = 2
}

public class Character : MonoBehaviour
{
    [SerializeField] private Transform worldWeaponSocket;

    private Transform runtimeWorldSocket;

    public WeaponType EquippedWeaponType { get; private set; }
    public Weapon EquippedWeapon { get; private set; }

    public void AttachTo(Transform parent)
    {
        transform.SetParent(parent, false);
        ResetLocalTransform();
    }

    public void ResetLocalTransform()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void ApplyPreviewSettings(int previewLayer, RuntimeAnimatorController controller)
    {
        DisableColliders();
        SetLayerRecursively(transform, previewLayer);

        var a = GetComponentInChildren<Animator>(true);
        if (a != null)
        {
            a.applyRootMotion = false;
            if (controller != null)
                a.runtimeAnimatorController = controller;
        }
    }

    public Transform GetWorldWeaponSocket()
    {
        if (worldWeaponSocket != null) return worldWeaponSocket;
        if (runtimeWorldSocket != null) return runtimeWorldSocket;

        Transform hand = WeaponAttachUtil.GetRightHand(transform);
        if (hand == null) return transform;

        var go = new GameObject("WorldWeaponSocket");
        go.transform.SetParent(hand, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        runtimeWorldSocket = go.transform;
        return runtimeWorldSocket;
    }

    public void AttachWeapon(WeaponType weaponType, Weapon weapon, Transform socket)
    {
        EquippedWeaponType = weaponType;
        EquippedWeapon = weapon;

        if (weapon == null) return;
        if (socket == null) socket = transform;

        weapon.transform.SetParent(socket, false);
    }

    public bool DetachWeapon(out WeaponType weaponType, out Weapon weapon)
    {
        weaponType = EquippedWeaponType;
        weapon = EquippedWeapon;

        EquippedWeaponType = default;
        EquippedWeapon = null;

        if (weapon == null) return false;

        weapon.transform.SetParent(null, false);
        return true;
    }

    private void DisableColliders()
    {
        var cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null) return;

        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }
}