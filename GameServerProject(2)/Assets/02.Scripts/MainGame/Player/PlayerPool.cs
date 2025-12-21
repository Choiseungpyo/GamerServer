using UnityEngine;

public sealed class PlayerPool : ObjectPoolBase<Player>
{
    private readonly Player prefab;
    private readonly Transform parent;

    public PlayerPool(Player prefab, Transform parent, int defaultCapacity, int maxSize)
        : base(defaultCapacity, maxSize)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    protected override Player CreateObject()
    {
        Player p = Object.Instantiate(prefab, parent);
        p.gameObject.SetActive(false);
        return p;
    }

    protected override void OnGet(Player obj)
    {
        obj.gameObject.SetActive(true);
    }

    protected override void OnRelease(Player obj)
    {
        obj.ResetForPool();
        obj.gameObject.SetActive(false);
    }

    protected override void OnDestroy(Player obj)
    {
        if (obj != null) 
            Object.Destroy(obj.gameObject);
    }
}