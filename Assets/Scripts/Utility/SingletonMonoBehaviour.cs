using UnityEngine;

public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    static T m_Instance;
    static object m_Lock = new object();

    public bool m_Persistent = false;
    public static T Instance
    {
        get
        {
            if (m_ApplicationIsQuitting)
            {
                Debug.LogWarning(string.Format(
                  "[Singleton] Instance '{0}' already destroyed on application quit. Won't create again - returning null.",
                  typeof(T)
                ));
                return null;
            }

            lock (m_Lock)
            {
                if (m_Instance == null)
                {
                    m_Instance = (T)FindObjectOfType(typeof(T));

                    if (FindObjectsOfType(typeof(T)).Length > 1)
                    {
                        Debug.LogError("[Singleton] Something went really wrong  - there should never be more than 1 singleton! Reopening the scene might fix it.");
                        return m_Instance;
                    }

                    if (m_Instance == null)
                    {
                        GameObject singleton = new GameObject();
                        m_Instance = singleton.AddComponent<T>();
                        singleton.name = "(singleton) " + typeof(T);

                        DontDestroyOnLoad(singleton);

                        Debug.Log(string.Format(
                          "[Singleton] An instance of {0} is needed in the scene, so '{1}' was created with DontDestroyOnLoad.",
                          typeof(T),
                          singleton
                        ));
                    }
                    else
                    {
                        Debug.Log(string.Format("[Singleton] Using instance already created: {0}", m_Instance.gameObject.name));
                    }
                }

                return m_Instance;
            }
        }
    }

    static bool m_ApplicationIsQuitting = false;
    void Awake()
    {
        if(m_Persistent)
        {
            DontDestroyOnLoad(this.gameObject);
        }
        m_Instance = (T)FindObjectOfType(typeof(T));
        if (null == m_Instance || FindObjectsOfType(typeof(T)).Length > 1)
        {
            Debug.LogError("[Singleton] Something went really wrong  - there should never be more than 1 singleton! Reopening the scene might fix it.");
        }
    }
    public void OnDestroy()
    {
        if (Debug.isDebugBuild)
        {
            Debug.Log(string.Format("[Singleton] {0} is being destroyed!", typeof(T)));
        }
        m_ApplicationIsQuitting = true;
    }
}

static public class MethodExtensionForMonoBehaviourTransform
{
    /// <summary>
    /// Gets or add a component. Usage example:
    /// BoxCollider boxCollider = transform.GetOrAddComponent<BoxCollider>();
    /// </summary>
    static public T GetOrAddComponent<T>(this Component child) where T : Component
    {
        T result = child.GetComponent<T>() ?? child.gameObject.AddComponent<T>();
        return result;
    }
}