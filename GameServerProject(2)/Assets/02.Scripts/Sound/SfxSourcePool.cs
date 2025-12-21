using UnityEngine;

public class SfxSourcePool : ObjectPoolBase<AudioSource>
{
    private readonly AudioSource prefab;
    private readonly Transform root;

    public SfxSourcePool(AudioSource prefab, Transform root, int defaultCapacity = 10, int maxSize = 100)
        : base(defaultCapacity, maxSize)
    {
        this.prefab = prefab;
        this.root = root;
    }

    protected override AudioSource CreateObject()
    {
        AudioSource src = Object.Instantiate(prefab, root);
        src.playOnAwake = false;
        src.loop = false;

        if (src.gameObject.activeSelf)
            src.gameObject.SetActive(false);

        return src;
    }

    protected override void OnGet(AudioSource obj)
    {
        if (obj == null) return;
        if (!obj.gameObject.activeSelf)
            obj.gameObject.SetActive(true);
    }

    protected override void OnRelease(AudioSource obj)
    {
        if (obj == null) return;

        obj.Stop();
        obj.clip = null;

        if (obj.gameObject.activeSelf)
            obj.gameObject.SetActive(false);
    }

    protected override void OnDestroy(AudioSource obj)
    {
        if (obj == null) return;
        Object.Destroy(obj.gameObject);
    }
}