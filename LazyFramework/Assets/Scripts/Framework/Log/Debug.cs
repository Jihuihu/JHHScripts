using System;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

/*
 * Server下直接使用FileLog的方式
 */
public class Debug
{
    public static FileLogger fileLog = null;

    public static void SetFileLog()
    {
#if !UNITY_EDITOR
       fileLog = new FileLogger("NormalLog.txt");
#endif
    }

    public static void Log(object message, UnityEngine.Object obj = null)
    {
        UnityEngine.Debug.Log(message.ToString(), obj);
        fileLog?.Log(message.ToString());
    }
	public static void LogWarning(object message, UnityEngine.Object obj = null)
    {
        UnityEngine.Debug.LogWarning(message, obj);
        fileLog?.Log("[Warning] "+message.ToString());
    }
    public static void LogError(object message, UnityEngine.Object obj = null)
    {
        UnityEngine.Debug.LogError(message, obj);
        fileLog?.Log("[Error] " + message.ToString());
    }

    public static void LogException(System.Exception e, UnityEngine.Object obj = null)
    {
        UnityEngine.Debug.LogException(e, obj);
        fileLog?.LogException(e, obj);
    }

    public static void LogFormat(string format,params object[] args)
	{
        var message = string.Format(format, args);
        UnityEngine.Debug.LogFormat(message);
        fileLog?.Log("[Error] " + message);
    }

    public static void LogWarningFormat(string format,params object[] args)
	{
        UnityEngine.Debug.LogWarningFormat(format, args);
    }

    public static void LogErrorFormat(string format,params object[] args)
	{
        UnityEngine.Debug.LogErrorFormat(format, args);
    }
}
