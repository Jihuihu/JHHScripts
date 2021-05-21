using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

public partial class GameLogicMgr : MonoBehaviour
{
    public static GameLogicMgr Instance => instance;
    private static GameLogicMgr instance = null;

    private void Awake()
    {
        instance = this;

        InitEnvironment();
        InitDevice();
    }

    void Start()
    {

    }

    void Update()
    {

    }

    private void InitDevice()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Debug.Log("Display: System : " + Display.main.systemWidth + "x" + Display.main.systemHeight + " / " + "rendering : " + Display.main.renderingWidth + "x" + Display.main.renderingHeight);
        Debug.Log("Screen.currentResolution is: " + Screen.currentResolution.width + "x" + Screen.currentResolution.height);

        Screen.orientation = ScreenOrientation.Portrait;

        //设置游戏帧率
        #if UNITY_STANDALONE_WIN
                Application.targetFrameRate = 60;
        #elif (UNITY_IPHONE || UNITY_ANDROID)
                Application.targetFrameRate = 30;
        #else
               Application.targetFrameRate = -1;
        #endif

        #if UNITY_EDITOR
                Caching.ClearCache();
#endif

        Debug.Log(GetDeviceInfo());
    }

    private void InitEnvironment()
    {

        Application.logMessageReceivedThreaded += ApplicationOnLogMessageReceivedThreaded;
        // 设置文件日志
        Debug.SetFileLog();
    }

    public static string GetDeviceInfo()
    {
        StringBuilder info = new StringBuilder();
        info.AppendLine("Device Info:");
        info.AppendLine("Device:");
        GetMessage(info, "Device Model", SystemInfo.deviceModel);
        GetMessage(info, "Device Name", SystemInfo.deviceName);
        GetMessage(info, "Devict Type", SystemInfo.deviceType.ToString());
        GetMessage(info, "System Memory(MB)", SystemInfo.systemMemorySize.ToString());
        GetMessage(info, "OS", SystemInfo.operatingSystem);
        GetMessage(info, "Device Identifier", SystemInfo.deviceUniqueIdentifier);

        info.AppendLine("CPU:");
        GetMessage(info, "CPU Type", SystemInfo.processorType);
        GetMessage(info, "CPU Count", SystemInfo.processorCount.ToString());
        GetMessage(info, "Frequency", SystemInfo.processorFrequency.ToString());

        info.AppendLine("GPU:");
        GetMessage(info, "GPU ID", SystemInfo.graphicsDeviceID.ToString());
        GetMessage(info, "GPU Name", SystemInfo.graphicsDeviceName);
        GetMessage(info, "GPU Type", SystemInfo.graphicsDeviceType.ToString());
        GetMessage(info, "GPU Vendor", SystemInfo.graphicsDeviceVendor);
        GetMessage(info, "GPU VendorID", SystemInfo.graphicsDeviceVendorID.ToString());
        GetMessage(info, "GPU Version", SystemInfo.graphicsDeviceVersion);
        GetMessage(info, "GPU Memory(MB)", SystemInfo.graphicsMemorySize.ToString());

        info.AppendLine("Textures:");
        GetMessage(info, "MaxTextureSize", SystemInfo.maxTextureSize.ToString());
        GetMessage(info, "NPOT Support", SystemInfo.npotSupport.ToString());

        info.AppendLine("Rendering:");
        GetMessage(info, "Support Shadows", SystemInfo.supportsShadows.ToString());
        GetMessage(info, "Support Stencil", SystemInfo.supportsStencil.ToString());
        GetMessage(info, "Support MultiThreaded", SystemInfo.graphicsMultiThreaded.ToString());
        GetMessage(info, "Support RenderTargetCount", SystemInfo.supportedRenderTargetCount.ToString());

        return info.ToString();
    }
    static void GetMessage(StringBuilder info, params string[] str)
    {
        if (str.Length == 2)
        {
            info.AppendLine("  " + str[0] + ":" + str[1]);
        }
    }
    

    private void ApplicationOnLogMessageReceivedThreaded(string condition, string stacktrace, LogType type)
    {
#if UNITY_EDITOR
        if (type == LogType.Exception)
        {
            var txt = UnityEngine.GameObject.Instantiate(Resources.Load<UnityEngine.UI.Text>("ExceptionText"));
            DontDestroyOnLoad(txt);
            var msg = string.Format("{0} \r\n {1}  \r\n{2} ", "调试中 请截图给研发人员 并重启游戏再次测试", condition, stacktrace);
            txt.text = msg;
        }
#endif
    }
}
