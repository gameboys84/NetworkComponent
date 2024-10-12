// using Fantasy;

using System.Collections.Generic;
using GameLogic;
using TPFramework;
using UnityEngine;

public class NetMain : SingletonUnity<NetMain>
{
    // private Fantasy.Scene _scene;
    // private Fantasy.Session _session;
    private bool _inited;
    
    private bool _addressRegisted = false;
    public bool AddressRegisted { get => _addressRegisted; set => _addressRegisted = value; }

    // public async FTask Initialize()
    public void Initialize()
    {
        if (_inited)
            return;
        
        _inited = true;
        // _scene = await Fantasy.Entry.Initialize(GetType().Assembly);
        
        GameObject netMain = GameObject.Find("NetMain");
        if (netMain == null)
        {
            netMain = new GameObject("NetMain");
            netMain.AddComponent<NetMain>();
        }
        
        GameObject go = new GameObject("NetManager");
        go.AddComponent<NetManager>();
        
        NetMessageMgr.Init();
        NetMessageMgr.onConnectCompleted += OnConnectCompleted;
        NetMessageMgr.onConnectFailed += OnConnectFailed;
        NetMessageMgr.onConnectDisconnected += OnConnectDisconnected;
    }

    // 127.0.0.1:20000, 5000
    public void Connect(string ip, int port, int timeout = 5000)
    {
        // _session = _scene.Connect(
        //     $"{ip}:{port}",
        //     // "127.0.0.1:20000",
        //     NetworkProtocolType.KCP,
        //     OnConnectComplete,
        //     OnConnectFail,
        //     OnConnectDisconnect,
        //     false, timeout);
        
        List<ProxyInfo> proxyList = new List<ProxyInfo>();
        ProxyInfo proxy = new ProxyInfo()
        {
            ip = ip,
            port = port,
            retryTime = 3,
        };
        proxyList.Add(proxy);
        NetMessageMgr.InitNet(proxyList);
    }

    private void OnConnectDisconnected()
    {
        Utils.Log("连接断开 Disconnect");
        OnDisconnected();
    }

    private void OnConnectFailed()
    {
        Utils.Log("连接失败 Fail");
        OnDisconnected();
    }

    private void OnDisconnected()
    {
        // _session.GetComponent<SessionHeartbeatComponent>().Dispose();
        // _session.RemoveComponent<SessionHeartbeatComponent>();
        AddressRegisted = false;
        // _session.Dispose();
        // _session = null;
        
        HeartBeatComponent.Instance.StopTimer();
    }

    private void OnConnectCompleted()
    {
        Utils.Log("<color=yellow>连接成功 Complete</color>");
        // 每interval 2秒向服务器发送一次心跳，用于向服务器保活
        // 本地每 timeOutInterval 3秒检测 上次服务器回应是否超时， 超时时间为 timeOut 2秒
        // _session.AddComponent<SessionHeartbeatComponent>().Start(2000);
        
        HeartBeatComponent.Instance.StartTimer();
    }

    public bool IsConnected()
    {
        return NetManager.GetSession().IsConnected();
        // return false;
        // return _session is {IsDisposed: false};
    }
}
