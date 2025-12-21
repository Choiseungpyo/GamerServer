using UnityEngine.Pool;

public abstract class ObjectPoolBase<T> where T : class
{
    private ObjectPool<T> pool;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="defaultCapacity">미리 만들어둘 객체 수</param>
    /// <param name="maxSize">풀 최대 용량</param>
    protected ObjectPoolBase(int defaultCapacity = 10, int maxSize = 100)
    {
        pool = new ObjectPool<T>(
            createFunc: CreateObject,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroy,
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    protected abstract T CreateObject();
    protected virtual void OnGet(T obj) { }
    protected virtual void OnRelease(T obj) { }
    protected virtual void OnDestroy(T obj) { }

    public T Get() => pool.Get();
    public void Release(T obj) => pool.Release(obj);
    public void Clear() => pool.Clear();
}
