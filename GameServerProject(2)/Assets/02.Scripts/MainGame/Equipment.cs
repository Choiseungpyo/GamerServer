using UnityEngine;

public class Equipment
{
    private readonly WeaponDatabase weaponDb;
    private readonly WeaponPool weaponPool;

    public Equipment(WeaponDatabase weaponDb, WeaponPool weaponPool)
    {
        this.weaponDb = weaponDb;
        this.weaponPool = weaponPool;
    }

    public Weapon Equip(Character character, WeaponType weaponType, WeaponViewMode mode, Transform socket)
    {
        if (character == null) return null;
        if (weaponDb == null) return null;
        if (weaponPool == null) return null;

        if (!weaponDb.TryGetVisual((int)weaponType, out var data) || data == null) return null;

        if (character.DetachWeapon(out var oldType, out var oldWeapon))
        {
            if (oldWeapon != null)
                weaponPool.Release(oldType, oldWeapon);
        }

        var weapon = weaponPool.Get(weaponType);
        if (weapon == null) return null;

        character.AttachWeapon(weaponType, weapon, socket);

        if (mode == WeaponViewMode.FirstPerson)
        {
            weapon.transform.localPosition = data.fpLocalPos;
            weapon.transform.localRotation = Quaternion.Euler(data.fpLocalEuler);
        }
        else
        {
            weapon.transform.localPosition = data.worldLocalPos;
            weapon.transform.localRotation = Quaternion.Euler(data.worldLocalEuler);
        }

        weapon.CacheMuzzle();
        return weapon;
    }

    public void Unequip(Character character)
    {
        if (character == null) return;

        if (character.DetachWeapon(out var oldType, out var oldWeapon))
        {
            if (oldWeapon != null)
                weaponPool.Release(oldType, oldWeapon);
        }
    }
}