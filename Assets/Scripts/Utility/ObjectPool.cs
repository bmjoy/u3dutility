using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public sealed class ObjectPool : SingletonMonoBehaviour<ObjectPool>
{
	public enum StartupPoolMode { Awake, Start, CallManually };

	[System.Serializable]
	public class StartupPool
	{
		public int m_Size;
		public GameObject m_Prefab;
	}

	static List<GameObject> m_TempList = new List<GameObject>();
	
	Dictionary<GameObject, List<GameObject>> m_PooledObjects = new Dictionary<GameObject, List<GameObject>>();
	Dictionary<GameObject, GameObject> m_SpawnedObjects = new Dictionary<GameObject, GameObject>();
	
	public StartupPoolMode m_StartupPoolMode;
	public StartupPool[] m_StartupPools;

	bool m_StartupPoolsCreated;

	void Awake()
	{
        if (StartupPoolMode.Awake == m_StartupPoolMode)
        {
            CreateStartupPools();
        }
	}

	void Start()
	{
        if (StartupPoolMode.Start == m_StartupPoolMode)
        {
            CreateStartupPools();
        }
	}

	public static void CreateStartupPools()
	{
        if (!Instance.m_StartupPoolsCreated)
		{
            Instance.m_StartupPoolsCreated = true;
            StartupPool[] pools = Instance.m_StartupPools;
            if (null != pools && 0 < pools.Length)
            {
                for (int i = 0; i < pools.Length; ++i)
                {
                    CreatePool(pools[i].m_Prefab, pools[i].m_Size);
                }
            }
		}
	}

	public static void CreatePool<T>(T prefab, int initialPoolSize) where T : Component
	{
		CreatePool(prefab.gameObject, initialPoolSize);
	}
	public static void CreatePool(GameObject prefab, int initialPoolSize)
	{
        if (null != prefab && !Instance.m_PooledObjects.ContainsKey(prefab))
		{
			List<GameObject> list = new List<GameObject>();
            Instance.m_PooledObjects.Add(prefab, list);

			if (0 < initialPoolSize)
			{
				bool active = prefab.activeSelf;
				prefab.SetActive(false);
                Transform parent = Instance.transform;
				while (list.Count < initialPoolSize)
				{
                    GameObject go = (GameObject)Object.Instantiate(prefab);
					go.transform.parent = parent;
					list.Add(go);
				}
				prefab.SetActive(active);
			}
		}
	}
	
	public static T Spawn<T>(T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component
	{
		return Spawn(prefab.gameObject, parent, position, rotation).GetComponent<T>();
	}
	public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
	{
		return Spawn(prefab.gameObject, null, position, rotation).GetComponent<T>();
	}
	public static T Spawn<T>(T prefab, Transform parent, Vector3 position) where T : Component
	{
		return Spawn(prefab.gameObject, parent, position, Quaternion.identity).GetComponent<T>();
	}
	public static T Spawn<T>(T prefab, Vector3 position) where T : Component
	{
		return Spawn(prefab.gameObject, null, position, Quaternion.identity).GetComponent<T>();
	}
	public static T Spawn<T>(T prefab, Transform parent) where T : Component
	{
		return Spawn(prefab.gameObject, parent, Vector3.zero, Quaternion.identity).GetComponent<T>();
	}
	public static T Spawn<T>(T prefab) where T : Component
	{
		return Spawn(prefab.gameObject, null, Vector3.zero, Quaternion.identity).GetComponent<T>();
	}
	public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
	{
		List<GameObject> list;
		Transform trans;
		GameObject go;
        if (Instance.m_PooledObjects.TryGetValue(prefab, out list))
		{
			go = null;
			if (0 < list.Count)
			{
				while (null == go && 0 < list.Count)
				{
					go = list[0];
					list.RemoveAt(0);
				}
				if (null != go)
				{
					trans = go.transform;
					trans.parent = parent;
					trans.localPosition = position;
					trans.localRotation = rotation;
					go.SetActive(true);
                    Instance.m_SpawnedObjects.Add(go, prefab);
					return go;
				}
			}
			go = (GameObject)Object.Instantiate(prefab);
			trans = go.transform;
			trans.parent = parent;
			trans.localPosition = position;
			trans.localRotation = rotation;
            Instance.m_SpawnedObjects.Add(go, prefab);
			return go;
		}
		else
		{
			go = (GameObject)Object.Instantiate(prefab);
			trans = go.GetComponent<Transform>();
			trans.parent = parent;
			trans.localPosition = position;
			trans.localRotation = rotation;
			return go;
		}
	}
	public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position)
	{
		return Spawn(prefab, parent, position, Quaternion.identity);
	}
	public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		return Spawn(prefab, null, position, rotation);
	}
	public static GameObject Spawn(GameObject prefab, Transform parent)
	{
		return Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
	}
	public static GameObject Spawn(GameObject prefab, Vector3 position)
	{
		return Spawn(prefab, null, position, Quaternion.identity);
	}
	public static GameObject Spawn(GameObject prefab)
	{
		return Spawn(prefab, null, Vector3.zero, Quaternion.identity);
	}

	public static void Recycle<T>(T go) where T : Component
	{
		Recycle(go.gameObject);
	}
	public static void Recycle(GameObject go)
	{
		GameObject prefab;
        if (Instance.m_SpawnedObjects.TryGetValue(go, out prefab))
        {
            Recycle(go, prefab);
        }
        else
        {
            Object.Destroy(go);
        }
	}
	static void Recycle(GameObject go, GameObject prefab)
	{
        Instance.m_PooledObjects[prefab].Add(go);
        Instance.m_SpawnedObjects.Remove(go);
        go.transform.parent = Instance.transform;
		go.SetActive(false);
	}

	public static void RecycleAll<T>(T prefab) where T : Component
	{
		RecycleAll(prefab.gameObject);
	}
	public static void RecycleAll(GameObject prefab)
	{
        foreach (KeyValuePair<GameObject, GameObject> kv in Instance.m_SpawnedObjects)
        {
            if (kv.Value == prefab)
            {
                m_TempList.Add(kv.Key);
            }
        }
        for (int i = 0; i < m_TempList.Count; ++i)
        {
            Recycle(m_TempList[i]);
        }
		m_TempList.Clear();
	}
	public static void RecycleAll()
	{
        m_TempList.AddRange(Instance.m_SpawnedObjects.Keys);
        for (int i = 0; i < m_TempList.Count; ++i)
        {
            Recycle(m_TempList[i]);
        }
		m_TempList.Clear();
	}
	
	public static bool IsSpawned(GameObject go)
	{
        return Instance.m_SpawnedObjects.ContainsKey(go);
	}

	public static int CountPooled<T>(T prefab) where T : Component
	{
		return CountPooled(prefab.gameObject);
	}
	public static int CountPooled(GameObject prefab)
	{
		List<GameObject> list;
        if (Instance.m_PooledObjects.TryGetValue(prefab, out list))
        {
            return list.Count;
        }
		return 0;
	}

	public static int CountSpawned<T>(T prefab) where T : Component
	{
		return CountSpawned(prefab.gameObject);
	}
	public static int CountSpawned(GameObject prefab)
	{
		int count = 0 ;
        foreach (GameObject instancePrefab in Instance.m_SpawnedObjects.Values)
        {
            if (prefab == instancePrefab)
            {
                ++count;
            }
        }
		return count;
	}

	public static int CountAllPooled()
	{
		int count = 0;
        foreach (List<GameObject> list in Instance.m_PooledObjects.Values)
        {
            count += list.Count;
        }
		return count;
	}

	public static List<GameObject> GetPooled(GameObject prefab, List<GameObject> list, bool appendList)
	{
        if (null == list)
        {
            list = new List<GameObject>();
        }
        if (!appendList)
        {
            list.Clear();
        }
		List<GameObject> pooled;
        if (Instance.m_PooledObjects.TryGetValue(prefab, out pooled))
        {
            list.AddRange(pooled);
        }
		return list;
	}
	public static List<T> GetPooled<T>(T prefab, List<T> list, bool appendList) where T : Component
	{
        if (null == list)
        {
            list = new List<T>();
        }
        if (!appendList)
        {
            list.Clear();
        }
		List<GameObject> pooled;
        if (Instance.m_PooledObjects.TryGetValue(prefab.gameObject, out pooled))
        {
            for (int i = 0; i < pooled.Count; ++i)
            {
                list.Add(pooled[i].GetComponent<T>());
            }
        }
		return list;
	}

	public static List<GameObject> GetSpawned(GameObject prefab, List<GameObject> list, bool appendList)
	{
        if (null == list)
        {
            list = new List<GameObject>();
        }
        if (!appendList)
        {
            list.Clear();
        }
        foreach (KeyValuePair<GameObject, GameObject> kv in Instance.m_SpawnedObjects)
        {
            if (kv.Value == prefab)
            {
                list.Add(kv.Key);
            }
        }
		return list;
	}
	public static List<T> GetSpawned<T>(T prefab, List<T> list, bool appendList) where T : Component
	{
        if (null == list)
        {
            list = new List<T>();
        }
        if (!appendList)
        {
            list.Clear();
        }
		GameObject prefabObj = prefab.gameObject;
        foreach (KeyValuePair<GameObject, GameObject> kv in Instance.m_SpawnedObjects)
        {
            if (kv.Value == prefabObj)
            {
                list.Add(kv.Key.GetComponent<T>());
            }
        }
		return list;
	}

	public static void DestroyPooled(GameObject prefab)
	{
		List<GameObject> pooled;
        if (Instance.m_PooledObjects.TryGetValue(prefab, out pooled))
		{
            for (int i = 0; i < pooled.Count; ++i)
            {
                GameObject.Destroy(pooled[i]);
            }
			pooled.Clear();
		}
	}
	public static void DestroyPooled<T>(T prefab) where T : Component
	{
		DestroyPooled(prefab.gameObject);
	}

	public static void DestroyAll(GameObject prefab)
	{
		RecycleAll(prefab);
		DestroyPooled(prefab);
	}
	public static void DestroyAll<T>(T prefab) where T : Component
	{
		DestroyAll(prefab.gameObject);
	}
}

public static class ObjectPoolExtensions
{
	public static void CreatePool<T>(this T prefab) where T : Component
	{
		ObjectPool.CreatePool(prefab, 0);
	}
	public static void CreatePool<T>(this T prefab, int initialPoolSize) where T : Component
	{
		ObjectPool.CreatePool(prefab, initialPoolSize);
	}
	public static void CreatePool(this GameObject prefab)
	{
		ObjectPool.CreatePool(prefab, 0);
	}
	public static void CreatePool(this GameObject prefab, int initialPoolSize)
	{
		ObjectPool.CreatePool(prefab, initialPoolSize);
	}
	
	public static T Spawn<T>(this T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component
	{
		return ObjectPool.Spawn(prefab, parent, position, rotation);
	}
	public static T Spawn<T>(this T prefab, Vector3 position, Quaternion rotation) where T : Component
	{
		return ObjectPool.Spawn(prefab, null, position, rotation);
	}
	public static T Spawn<T>(this T prefab, Transform parent, Vector3 position) where T : Component
	{
		return ObjectPool.Spawn(prefab, parent, position, Quaternion.identity);
	}
	public static T Spawn<T>(this T prefab, Vector3 position) where T : Component
	{
		return ObjectPool.Spawn(prefab, null, position, Quaternion.identity);
	}
	public static T Spawn<T>(this T prefab, Transform parent) where T : Component
	{
		return ObjectPool.Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
	}
	public static T Spawn<T>(this T prefab) where T : Component
	{
		return ObjectPool.Spawn(prefab, null, Vector3.zero, Quaternion.identity);
	}
	public static GameObject Spawn(this GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
	{
		return ObjectPool.Spawn(prefab, parent, position, rotation);
	}
	public static GameObject Spawn(this GameObject prefab, Vector3 position, Quaternion rotation)
	{
		return ObjectPool.Spawn(prefab, null, position, rotation);
	}
	public static GameObject Spawn(this GameObject prefab, Transform parent, Vector3 position)
	{
		return ObjectPool.Spawn(prefab, parent, position, Quaternion.identity);
	}
	public static GameObject Spawn(this GameObject prefab, Vector3 position)
	{
		return ObjectPool.Spawn(prefab, null, position, Quaternion.identity);
	}
	public static GameObject Spawn(this GameObject prefab, Transform parent)
	{
		return ObjectPool.Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
	}
	public static GameObject Spawn(this GameObject prefab)
	{
		return ObjectPool.Spawn(prefab, null, Vector3.zero, Quaternion.identity);
	}
	
	public static void Recycle<T>(this T go) where T : Component
	{
		ObjectPool.Recycle(go);
	}
	public static void Recycle(this GameObject go)
	{
		ObjectPool.Recycle(go);
	}

	public static void RecycleAll<T>(this T prefab) where T : Component
	{
		ObjectPool.RecycleAll(prefab);
	}
	public static void RecycleAll(this GameObject prefab)
	{
		ObjectPool.RecycleAll(prefab);
	}

	public static int CountPooled<T>(this T prefab) where T : Component
	{
		return ObjectPool.CountPooled(prefab);
	}
	public static int CountPooled(this GameObject prefab)
	{
		return ObjectPool.CountPooled(prefab);
	}

	public static int CountSpawned<T>(this T prefab) where T : Component
	{
		return ObjectPool.CountSpawned(prefab);
	}
	public static int CountSpawned(this GameObject prefab)
	{
		return ObjectPool.CountSpawned(prefab);
	}

	public static List<GameObject> GetSpawned(this GameObject prefab, List<GameObject> list, bool appendList)
	{
		return ObjectPool.GetSpawned(prefab, list, appendList);
	}
	public static List<GameObject> GetSpawned(this GameObject prefab, List<GameObject> list)
	{
		return ObjectPool.GetSpawned(prefab, list, false);
	}
	public static List<GameObject> GetSpawned(this GameObject prefab)
	{
		return ObjectPool.GetSpawned(prefab, null, false);
	}
	public static List<T> GetSpawned<T>(this T prefab, List<T> list, bool appendList) where T : Component
	{
		return ObjectPool.GetSpawned(prefab, list, appendList);
	}
	public static List<T> GetSpawned<T>(this T prefab, List<T> list) where T : Component
	{
		return ObjectPool.GetSpawned(prefab, list, false);
	}
	public static List<T> GetSpawned<T>(this T prefab) where T : Component
	{
		return ObjectPool.GetSpawned(prefab, null, false);
	}

	public static List<GameObject> GetPooled(this GameObject prefab, List<GameObject> list, bool appendList)
	{
		return ObjectPool.GetPooled(prefab, list, appendList);
	}
	public static List<GameObject> GetPooled(this GameObject prefab, List<GameObject> list)
	{
		return ObjectPool.GetPooled(prefab, list, false);
	}
	public static List<GameObject> GetPooled(this GameObject prefab)
	{
		return ObjectPool.GetPooled(prefab, null, false);
	}
	public static List<T> GetPooled<T>(this T prefab, List<T> list, bool appendList) where T : Component
	{
		return ObjectPool.GetPooled(prefab, list, appendList);
	}
	public static List<T> GetPooled<T>(this T prefab, List<T> list) where T : Component
	{
		return ObjectPool.GetPooled(prefab, list, false);
	}
	public static List<T> GetPooled<T>(this T prefab) where T : Component
	{
		return ObjectPool.GetPooled(prefab, null, false);
	}

	public static void DestroyPooled(this GameObject prefab)
	{
		ObjectPool.DestroyPooled(prefab);
	}
	public static void DestroyPooled<T>(this T prefab) where T : Component
	{
		ObjectPool.DestroyPooled(prefab.gameObject);
	}

	public static void DestroyAll(this GameObject prefab)
	{
		ObjectPool.DestroyAll(prefab);
	}
	public static void DestroyAll<T>(this T prefab) where T : Component
	{
		ObjectPool.DestroyAll(prefab.gameObject);
	}
}
