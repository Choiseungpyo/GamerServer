using UnityEngine;

public abstract class PoolableObject : MonoBehaviour
{
    public int ID { get; set; }

    // 각 클래스에서 개별적으로 설정
    protected abstract void OnSpawn();
    protected abstract void OnDespawn();



    // 풀에서 꺼낼 때 공통적으로 호출
    public void Spawn()
    {
        gameObject.SetActive(true);
        OnSpawn();
    }

    // 풀에 반환할 때 공통적으로 호출
    public void Despawn()
    {
        OnDespawn();
        gameObject.SetActive(false);
    }
}