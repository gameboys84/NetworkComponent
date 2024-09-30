using System;
using System.Collections;
using GameLogic;
using TPFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Boot : MonoBehaviour
{
    public static bool IsReboot => _isReboot;
    private static bool _isReboot = false;

    private void Awake()
    {
        if (_isReboot)
        {
            DoCleanup();
        }
        
        StartCoroutine(GameBootProcess());
        
    }

    private IEnumerator GameBootProcess()
    {
        // 初始化本地设置， 屏幕分辨率， 语言， 主题等
        
        // 请求权限， 如AppTrack
        
        // 数据清理， 自定义splash
        
        // 加载基础配置，多语言，设备信息
        
        // 热更新检测， 包括资源、代码等
        
        // 资源加载
        
        // 初始化各模块
        
        yield return new WaitForSeconds(1);
    }

    public static void Reboot()
    {
        Debug.LogWarning("Rebooting game...");
        _isReboot = true;
        
        SceneManager.LoadScene(0);
    }

    private void DoCleanup()
    {
        // 清理内存， 释放资源

        // 各模块清理自己的数据和资源
        NetMessageMgr.Reset();
    }

    void Start()
    {
        NetworkManager.Instance.Initialize(); // .Coroutine();
    }

}
