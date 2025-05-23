using UnityEngine;

public abstract class PoolableObject : MonoBehaviour
{
    public int ID { get; set; }

    // �� Ŭ�������� ���������� ����
    protected abstract void OnSpawn();
    protected abstract void OnDespawn();



    // Ǯ���� ���� �� ���������� ȣ��
    public void Spawn()
    {
        gameObject.SetActive(true);
        OnSpawn();
    }

    // Ǯ�� ��ȯ�� �� ���������� ȣ��
    public void Despawn()
    {
        OnDespawn();
        gameObject.SetActive(false);
    }
}