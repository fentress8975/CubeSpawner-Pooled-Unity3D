using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : MonoBehaviour
{
    private const int POOL_SIZE_STEP = 2;
    private const int TIME_TO_WAIT_AFTER_UNSUCCESSFUL_CLEANUP = 6;
    private const int TIME_TO_WAIT_AFTER_SUCCESSFUL_CLEANUP = 1;
    private const int POOL_MAX_REMOVE_COUNT = 40; //Try to not make lag spikes if there about 1000 objects to delete
    private CoroutinesHolder m_CoroutineHolder;

    private T m_Prefab;
    private Transform m_Parent;
    private List<T> m_Pool;

    private int m_Count;

    public int CurrentPoolSize { get { return m_Pool.Count; } }
    public ObjectPool(T prefab, int count)
    {
        m_Prefab = prefab;
        m_Count = count;
        CreateCoroutinesHolder();
        m_Parent = m_CoroutineHolder.transform;

        CreatePool();
    }

    public void ReturnToPool(T target)
    {
        target.transform.position = Vector3.zero;
        target.gameObject.SetActive(false);
    }

    public T GetObjectFromPool()
    {
        foreach (var item in m_Pool)
        {
            if (IsObjectReady(item))
            {
                item.gameObject.SetActive(true);
                return item;
            }
        }
        Debug.LogError("Oh no, no more objects in pool, RESIZE!");
        ChangePoolSize(m_Count + POOL_SIZE_STEP);
        return GetObjectFromPool();
    }

    public void ChangePoolSize(int poolSize)
    {
        var oldCount = m_Count;
        m_Count = poolSize;
        ResizePool(oldCount);
    }

    private void CreateCoroutinesHolder()
    {
        GameObject go = new($"Object Pool {m_Prefab.name}");
        m_CoroutineHolder = go.AddComponent<CoroutinesHolder>();
    }

    private void CreatePool()
    {
        m_Pool = new List<T>();

        for (int i = 0; i < m_Count; i++)
        {
            m_Pool.Add(CreateObject());
        }
    }

    private T CreateObject()
    {
        var createdObject = UnityEngine.Object.Instantiate(m_Prefab, m_Parent);
        createdObject.gameObject.SetActive(false);
        return createdObject;
    }

    private IEnumerator RemoveObjects(int count)
    {
        var removeCount = count;
        var removeMaxCount = POOL_MAX_REMOVE_COUNT;
        while (removeCount > 0)
        {
            if (TryRemoveObject())
            {
                Debug.Log("Trying to cleanup pool");
                removeCount--;
                removeMaxCount--;
                Debug.Log("Cleanup Succsess");
                if (removeMaxCount == 0)
                {
                    removeMaxCount = POOL_MAX_REMOVE_COUNT;
                    Debug.Log("Cleanup Cooldown");
                    yield return new WaitForSeconds(TIME_TO_WAIT_AFTER_SUCCESSFUL_CLEANUP);
                }
            }
            else
            {
                yield return new WaitForSeconds(TIME_TO_WAIT_AFTER_UNSUCCESSFUL_CLEANUP);
            }

        }
    }

    private bool TryRemoveObject()
    {

        if (IsObjectReady(m_Pool[^1]))
        {
            var go = m_Pool[^1];
            m_Pool.RemoveAt(m_Pool.Count - 1);
            UnityEngine.Object.Destroy(go.gameObject);
            return true;
        }
        else
        {
            Debug.Log("Cleanup Fail");
            return false;
        }
    }

    private bool IsObjectReady(T target)
    {
        return target.gameObject.activeInHierarchy == false ? true : false;
    }

    private void ResizePool(int oldCount)
    {
        if (oldCount == m_Count) return;
        m_CoroutineHolder.StopAllCoroutines();
        if (oldCount < m_Count)
        {
            for (int i = 0; i < m_Count - oldCount; i++)
            {
                m_Pool.Add(CreateObject());
            }
        }
        else
        {
            m_CoroutineHolder.StartCoroutine(RemoveObjects(oldCount - m_Count));
        }
    }
}
