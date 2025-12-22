using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerViewMode
{
    World,
    FirstPerson
}

public class Player : MonoBehaviour
{
    [Header("Rig")]
    [SerializeField] private Transform body;

    [Header("Model")]
    [SerializeField] private Transform modelRoot;

    [Header("FP Weapon Root")]
    [SerializeField] private Transform fpWeaponRoot;

    [Header("Controllers")]
    [SerializeField] private RuntimeAnimatorController gameController;

    [Header("Muzzle")]
    [SerializeField] private ParticleSystem muzzleFlash;

    [Header("Animator Params")]
    [SerializeField] private string moveDirParam = "MoveDir";
    [SerializeField] private float moveDeadZone = 0.1f;

    [Header("Shoot")]
    [SerializeField] private string shootParamName = "Shoot";
    [SerializeField] private float shootLockSeconds = 0.25f;

    [Header("Death")]
    [SerializeField] private string deadParamName = "IsDead";
    [SerializeField] private bool deadParamIsBool = true;
    [SerializeField] private float deathFallbackDisableDelay = 2.0f;

    private Transform muzzleFlashHomeParent;

    public Action<Player> OnDespawnRequested;
    public Action<int, int> OnHpChanged;
    public Action<int> OnDamaged;

    private ulong sessionId;

    private bool isLocalOwner;
    private bool isDead;

    private PlayerViewMode viewMode;

    private int moveDir;

    private bool shootLocked;
    private Coroutine shootLockCo;
    private Coroutine deathCo;

    public bool CanMove => !isDead && !shootLocked;

    public ulong SessionId => sessionId;
    public bool IsLocal => isLocalOwner;
    public bool IsDead => isDead;

    public int WeaponId { get; private set; }
    public int Hp { get; private set; }
    public int MaxHp { get; private set; }

    public Transform CameraPivot;

    private Transform muzzle;
    private Animator activeAnimator;

    private Character activeCharacter;
    private CharacterType activeCharacterType;

    public Vector3 MuzzlePosition
    {
        get
        {
            if (muzzle != null) return muzzle.position;
            if (CameraPivot != null) return CameraPivot.position;
            return transform.position;
        }
    }

    public Vector3 AimForward
    {
        get
        {
            if (CameraPivot != null) return CameraPivot.forward;
            return transform.forward;
        }
    }

    private void Awake()
    {
        if (muzzleFlash != null)
        {
            muzzleFlashHomeParent = muzzleFlash.transform.parent;
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.gameObject.SetActive(false);
        }
    }

    private void NotifyHpChanged()
    {
        if (OnHpChanged != null)
            OnHpChanged.Invoke(Hp, MaxHp);
    }

    public void Spawn(ulong sid, bool localOwner, Vector3 pos)
    {
        sessionId = sid;
        isLocalOwner = localOwner;

        isDead = false;
        shootLocked = false;

        gameObject.SetActive(true);
        transform.position = pos;

        ResetMuzzleFlashHome();

        MaxHp = 100;
        Hp = 100;
        NotifyHpChanged();

        WeaponId = 0;
        muzzle = null;

        if (deathCo != null) { StopCoroutine(deathCo); deathCo = null; }
        if (shootLockCo != null) { StopCoroutine(shootLockCo); shootLockCo = null; }

        if (activeAnimator != null && !string.IsNullOrEmpty(deadParamName) && deadParamIsBool)
            activeAnimator.SetBool(deadParamName, false);

        SetMoveDirInternal(0);

        SetObservedByLocalCamera(localOwner);
    }

    public void SetObservedByLocalCamera(bool observed)
    {
        viewMode = observed ? PlayerViewMode.FirstPerson : PlayerViewMode.World;

        if (activeCharacter != null)
        {
            ApplyViewModeToRenderers();
            ReapplyWeaponForViewMode();
        }
    }

    private WeaponViewMode GetWeaponViewMode()
    {
        return (viewMode == PlayerViewMode.FirstPerson) ? WeaponViewMode.FirstPerson : WeaponViewMode.World;
    }

    private Transform GetWeaponSocketByMode(WeaponViewMode mode)
    {
        if (activeCharacter == null) return null;

        if (mode == WeaponViewMode.FirstPerson)
        {
            if (fpWeaponRoot != null) return fpWeaponRoot;
            if (CameraPivot != null) return CameraPivot;
            return transform;
        }

        return activeCharacter.GetWorldWeaponSocket();
    }

    private void ReapplyWeaponForViewMode()
    {
        int wid = WeaponId;
        if (wid <= 0) return;
        SetWeapon(wid);
    }

    public void ApplyVisual(int characterId, int weaponId)
    {
        if (characterId <= 0)
        {
            ClearVisual();
            return;
        }

        ReleaseVisual();

        var dm = DataManager.Instance;
        if (dm == null) return;

        activeCharacterType = (CharacterType)characterId;
        activeCharacter = dm.CharacterPool.Get(activeCharacterType);
        if (activeCharacter == null) return;

        if (modelRoot == null) modelRoot = body;
        if (modelRoot != null)
            activeCharacter.AttachTo(modelRoot);

        activeAnimator = activeCharacter.GetComponentInChildren<Animator>(true);
        if (activeAnimator != null)
        {
            activeAnimator.applyRootMotion = false;
            if (gameController != null)
                activeAnimator.runtimeAnimatorController = gameController;

            if (!string.IsNullOrEmpty(deadParamName) && deadParamIsBool)
                activeAnimator.SetBool(deadParamName, isDead);
        }

        ApplyViewModeToRenderers();
        SetWeapon(weaponId);
    }

    private void SetWeapon(int weaponId)
    {
        var dm = DataManager.Instance;
        if (dm == null) return;
        if (activeCharacter == null) return;
        if (dm.Equipment == null) return;

        WeaponId = weaponId;

        dm.Equipment.Unequip(activeCharacter);
        muzzle = null;

        ResetMuzzleFlashHome();

        if (weaponId <= 0)
            return;

        WeaponViewMode mode = GetWeaponViewMode();
        Transform socket = GetWeaponSocketByMode(mode);

        Weapon w = dm.Equipment.Equip(activeCharacter, (WeaponType)weaponId, mode, socket);

        if (w != null)
            muzzle = w.Muzzle;

        AttachMuzzleFlashIfPossible();

        ApplyViewModeToRenderers();
    }

    private void ApplyViewModeToRenderers()
    {
        if (activeCharacter == null) return;

        bool bodyVisible = (viewMode == PlayerViewMode.World);

        var rs = activeCharacter.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++)
        {
            var r = rs[i];
            if (r == null) continue;
            r.enabled = bodyVisible;
        }
    }

    private void ResetMuzzleFlashHome()
    {
        if (muzzleFlash == null) return;

        muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        muzzleFlash.gameObject.SetActive(false);

        if (muzzleFlashHomeParent != null)
            muzzleFlash.transform.SetParent(muzzleFlashHomeParent, false);
    }

    private void AttachMuzzleFlashIfPossible()
    {
        if (muzzleFlash == null) return;
        if (muzzle == null) return;

        Transform t = muzzleFlash.transform;
        t.SetParent(muzzle, false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        muzzleFlash.gameObject.SetActive(false);
    }

    private void ReleaseVisual()
    {
        var dm = DataManager.Instance;

        Character c = activeCharacter;
        CharacterType ct = activeCharacterType;

        activeCharacter = null;
        activeCharacterType = default;

        activeAnimator = null;
        muzzle = null;

        ResetMuzzleFlashHome();

        if (c == null) return;

        var rs = c.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++)
            if (rs[i] != null) rs[i].enabled = true;

        if (dm != null && dm.Equipment != null)
            dm.Equipment.Unequip(c);

        if (dm != null && dm.CharacterPool != null)
        {
            dm.CharacterPool.Release(ct, c);
        }
        else
        {
            c.gameObject.SetActive(false);
            c.transform.SetParent(null, false);
        }
    }

    public void ClearVisual()
    {
        ReleaseVisual();
        WeaponId = 0;
    }

    public void ApplyServerState(Vector3 pos, float yaw, float pitch, int hp, int weaponId)
    {
        Vector3 prev = transform.position;
        transform.position = pos;

        if (body != null)
            body.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (CameraPivot != null)
            CameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        int prevWeapon = WeaponId;
        if (weaponId != prevWeapon)
            SetWeapon(weaponId);

        int prevHp = Hp;
        Hp = hp;

        if (Hp < prevHp && OnDamaged != null)
            OnDamaged.Invoke(prevHp - Hp);

        if (Hp != prevHp)
            NotifyHpChanged();

        if (!isDead && prevHp > 0 && Hp <= 0)
            StartDeath();

        UpdateMoveFromDeltaWorld(pos - prev);
    }

    public void PlayMuzzleFlash()
    {
        if (muzzleFlash == null) return;
        if (muzzle == null) return;

        muzzleFlash.gameObject.SetActive(true);
        muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        muzzleFlash.Play(true);
    }

    private void StartDeath()
    {
        isDead = true;
        SetMoveDirInternal(0);

        if (deathCo != null)
        {
            StopCoroutine(deathCo);
            deathCo = null;
        }

        if (activeAnimator != null && !string.IsNullOrEmpty(deadParamName))
        {
            if (deadParamIsBool)
                activeAnimator.SetBool(deadParamName, true);
            else
                activeAnimator.SetTrigger(deadParamName);
        }

        deathCo = StartCoroutine(DeathFallbackCoroutine());
    }

    private IEnumerator DeathFallbackCoroutine()
    {
        yield return new WaitForSeconds(deathFallbackDisableDelay);
        RequestDespawn();
    }

    private void RequestDespawn()
    {
        if (OnDespawnRequested != null)
            OnDespawnRequested(this);
        else
            gameObject.SetActive(false);
    }

    public void ResetForPool()
    {
        isDead = false;
        shootLocked = false;

        if (shootLockCo != null) { StopCoroutine(shootLockCo); shootLockCo = null; }
        if (deathCo != null) { StopCoroutine(deathCo); deathCo = null; }

        ClearVisual();

        sessionId = 0;
        isLocalOwner = false;

        MaxHp = 0;
        Hp = 0;
        WeaponId = 0;

        OnDespawnRequested = null;
        OnHpChanged = null;
        OnDamaged = null;

        SetMoveDirInternal(0);

        viewMode = PlayerViewMode.World;
    }

    public void Despawn()
    {
        ResetForPool();
        gameObject.SetActive(false);
    }

    public void SetHp(int hp)
    {
        int prevHp = Hp;
        Hp = hp;

        if (Hp < prevHp && OnDamaged != null)
            OnDamaged.Invoke(prevHp - Hp);

        if (Hp != prevHp)
            NotifyHpChanged();

        if (!isDead && prevHp > 0 && Hp <= 0)
            StartDeath();
    }

    public void SetMoveInput(float moveX, float moveZ)
    {
        if (activeAnimator == null) return;
        if (isDead) return;

        if (shootLocked)
        {
            SetMoveDirInternal(0);
            return;
        }

        float ax = Mathf.Abs(moveX);
        float az = Mathf.Abs(moveZ);

        int dir = 0;

        if (ax < moveDeadZone && az < moveDeadZone)
        {
            dir = 0;
        }
        else
        {
            if (az >= ax)
                dir = (moveZ >= 0f) ? 1 : 2;
            else
                dir = (moveX >= 0f) ? 4 : 3;
        }

        SetMoveDirInternal(dir);
    }

    private void SetMoveDirInternal(int dir)
    {
        moveDir = dir;

        if (activeAnimator != null && !string.IsNullOrEmpty(moveDirParam))
            activeAnimator.SetInteger(moveDirParam, moveDir);
    }

    private void UpdateMoveFromDeltaWorld(Vector3 deltaWorld)
    {
        if (activeAnimator == null) return;
        if (isDead) { SetMoveDirInternal(0); return; }

        Vector3 d = new Vector3(deltaWorld.x, 0f, deltaWorld.z);
        if (d.sqrMagnitude < 1e-6f)
        {
            SetMoveDirInternal(0);
            return;
        }

        if (body == null)
        {
            SetMoveInput(d.x, d.z);
            return;
        }

        Vector3 local = body.InverseTransformDirection(d.normalized);
        SetMoveInput(local.x, local.z);
    }

    public void PlayShoot()
    {
        if (activeAnimator != null && !string.IsNullOrEmpty(shootParamName))
        {
            activeAnimator.ResetTrigger(shootParamName);
            activeAnimator.SetTrigger(shootParamName);
        }

        StartShootLock();
    }

    private void StartShootLock()
    {
        if (shootLockCo != null)
        {
            StopCoroutine(shootLockCo);
            shootLockCo = null;
        }

        shootLockCo = StartCoroutine(ShootLockCoroutine());
    }

    private IEnumerator ShootLockCoroutine()
    {
        shootLocked = true;
        SetMoveDirInternal(0);

        yield return new WaitForSeconds(shootLockSeconds);

        shootLocked = false;
        shootLockCo = null;
    }

    public void SetLook(float yaw, float pitch)
    {
        if (body != null)
            body.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (CameraPivot != null)
            CameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}