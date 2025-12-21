using UnityEngine;

public class PlayerPoolComponent : MonoBehaviour
{
    [SerializeField] private Player prefab;
    [SerializeField] private int defaultCapacity = 3;
    [SerializeField] private int maxSize = 8;

    private PlayerPool pool;

    private void Awake()
    {
        pool = new PlayerPool(prefab, transform, defaultCapacity, maxSize);
    }

    public Player Get() => pool.Get();
    public void Release(Player p) => pool.Release(p);
    public void Clear() => pool.Clear();
}