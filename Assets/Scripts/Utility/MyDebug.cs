using UnityEngine;
using System.Collections;

public class MyDebug{
    public static void Log(object message)
    {
        if(!Debug.isDebugBuild)
        {
            return;
        }
        Debug.Log(message);
    }

    public static void Log(object message, Object context)
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }
        Debug.Log(message, context);
    }

    public static void LogWarning(object message)
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }
        Debug.LogWarning(message);
    }

    public static void LogWarning(object message, UnityEngine.Object context)
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }
        Debug.LogWarning(message, context);
    }

    public static void LogError(object message)
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }
        Debug.LogError(message);
    }

    public static void LogError(object message, UnityEngine.Object context)
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }
        Debug.LogError(message, context);
    }
    public static void LogException(System.Exception e)
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }
        Debug.LogException(e);
    }
    public static void Logexception(System.Exception e, UnityEngine.Object context)
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }
        Debug.LogException(e, context);
    }
}
