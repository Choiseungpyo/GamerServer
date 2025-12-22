using UnityEngine;

public class CharacterPreviewRig : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Transform modelRoot;
    [SerializeField] private RuntimeAnimatorController previewController;

    private RenderTexture rt;
    private int previewLayer;

    private Character activeCharacter;
    private CharacterType activeCharacterType;

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

    private void OnDisable()
    {
        ReleaseActive();
    }

    public void SetCharacter(int characterId, int weaponIdFromServer)
    {
        var dm = DataManager.Instance;
        if (dm == null) return;

        ReleaseActive();

        activeCharacterType = (CharacterType)characterId;

        activeCharacter = dm.CharacterPool.Get(activeCharacterType);
        if (activeCharacter == null) return;

        if (modelRoot != null) activeCharacter.AttachTo(modelRoot);

        activeCharacter.ApplyPreviewSettings(previewLayer, previewController);

        if (weaponIdFromServer > 0)
            dm.Equipment.Equip(activeCharacter, (WeaponType)weaponIdFromServer, WeaponViewMode.World, activeCharacter.GetWorldWeaponSocket());
    }

    private void ReleaseActive()
    {
        var dm = DataManager.Instance;
        if (dm == null) return;

        if (activeCharacter == null) return;

        if (dm.Equipment != null)
            dm.Equipment.Unequip(activeCharacter);

        if (dm.CharacterPool != null)
            dm.CharacterPool.Release(activeCharacterType, activeCharacter);

        activeCharacter = null;
        activeCharacterType = default;
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
}