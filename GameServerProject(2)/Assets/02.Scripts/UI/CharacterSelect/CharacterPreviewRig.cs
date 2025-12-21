using System.Collections.Generic;
using UnityEngine;

public class CharacterPreviewRig : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Transform modelRoot;
    [SerializeField] private RuntimeAnimatorController previewController;

    [Header("Weapon DB")]
    [SerializeField] private WeaponDatabaseSO weaponDb;

    private RenderTexture rt;
    private int previewLayer;

    private readonly Dictionary<GameObject, GameObject> modelCache = new Dictionary<GameObject, GameObject>(NetConst.MAX_CHARACTERS);
    private readonly Dictionary<int, GameObject> weaponCacheById = new Dictionary<int, GameObject>(NetConst.MAX_WEAPONS);

    private GameObject activeModel;
    private GameObject activeWeapon;

    private void Awake()
    {
        if (weaponDb == null && DataManager.Instance != null)
            weaponDb = DataManager.Instance.WeaponVisualDb;

        if (weaponDb != null)
            weaponDb.Build();
    }

    public void Setup(RenderTexture targetTexture, LayerMask cullingMask)
    {
        rt = targetTexture;

        if (cam != null)
        {
            cam.targetTexture = rt;
            cam.cullingMask = cullingMask;
        }

        previewLayer = LayerMaskToSingleLayer(cullingMask);
    }

    public void SetCharacter(GameObject modelPrefab, int weaponId)
    {
        SetModelInternal(modelPrefab);
        SetWeaponInternalById(weaponId);
    }

    public void SetModel(GameObject prefab)
    {
        SetModelInternal(prefab);
    }

    private void SetModelInternal(GameObject prefab)
    {
        if (activeModel != null)
            activeModel.SetActive(false);

        activeModel = null;

        if (prefab == null)
            return;

        if (!modelCache.TryGetValue(prefab, out var inst) || inst == null)
        {
            inst = Instantiate(prefab, modelRoot);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            modelCache[prefab] = inst;
        }

        activeModel = inst;
        activeModel.SetActive(true);

        SetLayerRecursively(activeModel.transform, previewLayer);

        var a = activeModel.GetComponentInChildren<Animator>(true);
        if (a != null)
        {
            a.applyRootMotion = false;
            if (previewController != null)
                a.runtimeAnimatorController = previewController;
        }
    }

    private void SetWeaponInternalById(int weaponId)
    {
        if (activeWeapon != null)
            activeWeapon.SetActive(false);

        activeWeapon = null;

        if (weaponId <= 0)
            return;

        if (weaponDb == null)
            return;

        if (activeModel == null)
            return;

        if (!weaponDb.TryGet(weaponId, out var row))
            return;

        if (row == null || row.worldPrefab == null)
            return;

        Transform hand = WeaponAttachUtil.GetRightHand(activeModel.transform);
        if (hand == null)
            hand = activeModel.transform;

        if (!weaponCacheById.TryGetValue(weaponId, out var w) || w == null)
        {
            w = Instantiate(row.worldPrefab);
            weaponCacheById[weaponId] = w;
        }

        w.transform.SetParent(hand, false);
        w.transform.localPosition = row.worldLocalPos;
        w.transform.localRotation = Quaternion.Euler(row.worldLocalEuler);

        SetLayerRecursively(w.transform, previewLayer);

        activeWeapon = w;
        activeWeapon.SetActive(true);
    }

    private static int LayerMaskToSingleLayer(LayerMask mask)
    {
        int m = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((m & (1 << i)) != 0)
                return i;
        }
        return 0;
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null) return;
        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }
}