using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Rig")]
    [SerializeField] private Transform body;
    [SerializeField] private Transform cameraPivot;

    [Header("Model")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Transform muzzleFallback;

    [Header("Controllers")]
    [SerializeField] private RuntimeAnimatorController gameController;

    [Header("Weapon DB")]
    [SerializeField] private WeaponDatabaseSO weaponDb;

    [Header("Weapon First Person Root")]
    [SerializeField] private Transform weaponRoot;

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

    private readonly Dictionary<int, GameObject> modelCache = new Dictionary<int, GameObject>(NetConst.MAX_CHARACTERS);
    private readonly Dictionary<int, GameObject> weaponWorldCache = new Dictionary<int, GameObject>(NetConst.MAX_CHARACTERS);
    private readonly Dictionary<int, GameObject> weaponFpCache = new Dictionary<int, GameObject>(NetConst.MAX_WEAPONS);

    private GameObject activeModel;
    private Animator activeAnimator;

    private GameObject activeWorldWeapon;
    private GameObject activeFpWeapon;

    private Transform muzzle;

    private ulong sessionId;
    private bool isLocal;
    private bool isDead;
    private Coroutine deathCo;

    private int moveDir;

    private bool shootLocked;
    private Coroutine shootLockCo;

    public bool CanMove => !isDead && !shootLocked;

    public ulong SessionId => sessionId;
    public bool IsLocal => isLocal;
    public bool IsDead => isDead;

    public int WeaponId { get; private set; }
    public int Hp { get; private set; }
    public int MaxHp { get; private set; }


    public Vector3 MuzzlePosition
    {
        get
        {
            if (muzzle != null) return muzzle.position;
            if (muzzleFallback != null) return muzzleFallback.position;
            if (cameraPivot != null) return cameraPivot.position;
            return transform.position;
        }
    }

    public Vector3 AimForward
    {
        get
        {
            if (cameraPivot != null) return cameraPivot.forward;
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
        OnHpChanged?.Invoke(Hp, MaxHp);
    }

    public void Spawn(ulong sid, bool local, Vector3 pos)
    {
        sessionId = sid;
        isLocal = local;
        isDead = false;
        shootLocked = false;

        gameObject.SetActive(true);
        transform.position = pos;


        if (cameraPivot != null)
        {
            float y = isLocal ? 0.89f : 1.5f;
            cameraPivot.localPosition = new Vector3(0f, y, 0f);
            cameraPivot.localRotation = Quaternion.identity;
        }

        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.gameObject.SetActive(false);
            if (muzzleFlashHomeParent != null)
                muzzleFlash.transform.SetParent(muzzleFlashHomeParent, false);
        }

        MaxHp = 100;
        Hp = 100;
        NotifyHpChanged();

        WeaponId = 0;

        DisableActiveWeapons();

        if (deathCo != null) { StopCoroutine(deathCo); deathCo = null; }
        if (shootLockCo != null) { StopCoroutine(shootLockCo); shootLockCo = null; }
        
        if (activeAnimator != null && !string.IsNullOrEmpty(deadParamName) && deadParamIsBool)
            activeAnimator.SetBool(deadParamName, false);


        SetMoveDirInternal(0);
    }

    public void BindMainCamera(Camera cam)
    {
        if (cam == null) return;
        if (cameraPivot == null) return;

        Transform t = cam.transform;
        t.SetParent(cameraPivot, false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
    }

    public void SetLook(float yaw, float pitch)
    {
        if (body != null)
            body.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void ApplyServerState(Vector3 pos, float yaw, float pitch, int hp, int weaponId)
    {
        Vector3 prev = transform.position;
        transform.position = pos;

        if (body != null)
            body.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        int prevWeapon = WeaponId;

        if (weaponId > 0)
        {
            WeaponId = weaponId;
            if (prevWeapon != WeaponId)
                ApplyWeaponVisualById(WeaponId);
        }

        int prevHp = Hp;
        Hp = hp;

        if (Hp < prevHp)
            OnDamaged?.Invoke(prevHp - Hp);

        if (Hp != prevHp)
            NotifyHpChanged();

        if (!isDead && prevHp > 0 && Hp <= 0)
            StartDeath();

        UpdateMoveFromDeltaWorld(pos - prev);
    }

    // GameSessionManager/ShotResult에서 쓰는 용도
    public void SetHp(int hp)
    {
        int prevHp = Hp;
        Hp = hp;

        if (Hp < prevHp)
            OnDamaged?.Invoke(prevHp - Hp);

        if (Hp != prevHp)
            NotifyHpChanged();

        if (!isDead && prevHp > 0 && Hp <= 0)
            StartDeath();
    }

    // GameSessionManager에서 기본무기 적용하려고 부르는 함수
    public void SetDefaultWeapon(int weaponId)
    {
        WeaponId = weaponId;
        ApplyWeaponVisualById(WeaponId);
    }

    public void SetCharacterModel(int characterId, GameObject modelPrefab)
    {
        if (modelRoot == null) modelRoot = body;
        if (modelRoot == null) return;

        if (activeModel != null)
            activeModel.SetActive(false);

        muzzle = null;
        activeAnimator = null;

        if (modelPrefab == null) return;

        if (!modelCache.TryGetValue(characterId, out var m) || m == null)
        {
            m = Instantiate(modelPrefab, modelRoot);
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.identity;
            m.transform.localScale = Vector3.one;
            modelCache[characterId] = m;
        }

        activeModel = m;
        activeModel.SetActive(true);

        EnableAllRenderers(activeModel, true);

        activeAnimator = activeModel.GetComponentInChildren<Animator>(true);
        if (activeAnimator != null)
        {
            activeAnimator.applyRootMotion = false;
            if (gameController != null)
                activeAnimator.runtimeAnimatorController = gameController;

            if (!string.IsNullOrEmpty(deadParamName) && deadParamIsBool)
                activeAnimator.SetBool(deadParamName, isDead);
        }

        if (isLocal)
            HideLocalBodyRenderers();

        ApplyWeaponVisualById(WeaponId);
    }

    private void ApplyWeaponVisualById(int weaponId)
    {
        if (weaponDb == null) return;
        if (!weaponDb.TryGet(weaponId, out var row)) return;
        EquipWeapon(row);
    }

    private void EquipWeapon(WeaponRow row)
    {
        if (row == null) return;

        DisableActiveWeapons();

        if (isLocal)
        {
            if (weaponRoot == null) return;

            GameObject prefab = row.fpPrefab != null ? row.fpPrefab : row.worldPrefab;
            if (prefab == null) return;

            int id = row.weaponId;

            if (!weaponFpCache.TryGetValue(id, out var w) || w == null)
            {
                w = Instantiate(prefab, weaponRoot);
                weaponFpCache[id] = w;
            }

            activeFpWeapon = w;
            activeFpWeapon.SetActive(true);

            activeFpWeapon.transform.SetParent(weaponRoot, false);
            activeFpWeapon.transform.localPosition = row.fpLocalPos;
            activeFpWeapon.transform.localRotation = Quaternion.Euler(row.fpLocalEuler);

            muzzle = WeaponAttachUtil.GetMuzzle(activeFpWeapon.transform, row.muzzleName);
            if (muzzle == null) muzzle = muzzleFallback;

            AttachMuzzleFlashIfPossible();
        }
        else
        {
            if (activeModel == null) return;
            if (row.worldPrefab == null) return;

            Transform hand = WeaponAttachUtil.GetRightHand(activeModel.transform);
            if (hand == null) hand = activeModel.transform;

            int id = row.weaponId;

            if (!weaponWorldCache.TryGetValue(id, out var w) || w == null)
            {
                w = Instantiate(row.worldPrefab, hand);
                weaponWorldCache[id] = w;
            }

            activeWorldWeapon = w;
            activeWorldWeapon.SetActive(true);

            activeWorldWeapon.transform.SetParent(hand, false);
            activeWorldWeapon.transform.localPosition = row.worldLocalPos;
            activeWorldWeapon.transform.localRotation = Quaternion.Euler(row.worldLocalEuler);

            muzzle = WeaponAttachUtil.GetMuzzle(activeWorldWeapon.transform, row.muzzleName);
            if (muzzle == null) muzzle = muzzleFallback;

            AttachMuzzleFlashIfPossible();
        }

        WeaponId = row.weaponId;
    }

    private void DisableActiveWeapons()
    {
        if (activeWorldWeapon != null) activeWorldWeapon.SetActive(false);
        if (activeFpWeapon != null) activeFpWeapon.SetActive(false);
        activeWorldWeapon = null;
        activeFpWeapon = null;
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

    private static void EnableAllRenderers(GameObject root, bool on)
    {
        if (root == null) return;
        var rs = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++)
            if (rs[i] != null) rs[i].enabled = on;
    }

    private void HideLocalBodyRenderers()
    {
        if (activeModel == null) return;

        var renderers = activeModel.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            r.enabled = false;
        }
    }

    public void PlayMuzzleFlash()
    {
        if (muzzleFlash == null) return;

        muzzleFlash.gameObject.SetActive(true);
        muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        muzzleFlash.Play(true);
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

        if (activeAnimator != null && !string.IsNullOrEmpty(deadParamName) && deadParamIsBool)
            activeAnimator.SetBool(deadParamName, false);

        DisableActiveWeapons();

        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.gameObject.SetActive(false);
            if (muzzleFlashHomeParent != null)
                muzzleFlash.transform.SetParent(muzzleFlashHomeParent, false);
        }

        foreach (var kv in modelCache)
        {
            if (kv.Value != null)
            {
                EnableAllRenderers(kv.Value, true);
                kv.Value.SetActive(false);
            }
        }

        activeModel = null;
        activeAnimator = null;
        muzzle = null;

        sessionId = 0;
        isLocal = false;

        MaxHp = 0;
        Hp = 0;
        WeaponId = 0;

        OnDespawnRequested = null;
        OnHpChanged = null;
        OnDamaged = null;

        SetMoveDirInternal(0);
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
}