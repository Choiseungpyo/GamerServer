using UnityEngine;

public class WeaponPool : MultipleObjectPoolBase<WeaponType, Weapon>
{
    private readonly WeaponDatabase weaponDb;
    private readonly Transform poolRoot;

    public WeaponPool(WeaponDatabase weaponDb, Transform poolRoot, int defaultCapacity = 10, int maxSize = 64, bool collectionCheck = false)
        : base(defaultCapacity, maxSize, collectionCheck)
    {
        this.weaponDb = weaponDb;
        this.poolRoot = poolRoot;
    }

    protected override Weapon CreateItem(WeaponType type)
    {
        if (weaponDb == null) return null;

        if (!weaponDb.TryGetVisual((int)type, out var data)) return null;
        if (data == null || data.prefab == null) return null;

        var go = Object.Instantiate(data.prefab, poolRoot);
        go.SetActive(false);

        var weapon = go.GetComponent<Weapon>();
        if (weapon == null)
        {
            Object.Destroy(go);
            return null;
        }

        return weapon;
    }

    protected override void OnGet(WeaponType type, Weapon component)
    {
        if (component == null) return;
        component.gameObject.SetActive(true);
    }

    protected override void OnRelease(WeaponType type, Weapon component)
    {
        if (component == null) return;
        component.transform.SetParent(poolRoot, false);
        component.gameObject.SetActive(false);
    }
}