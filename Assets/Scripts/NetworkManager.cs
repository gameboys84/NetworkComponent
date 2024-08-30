// using Fantasy;
using GameLogic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance => _instance;
    // public Fantasy.Session Session => _session;
    
    private static NetworkManager _instance;
    // private Fantasy.Scene _scene;
    // private Fantasy.Session _session;
    private bool _inited;
    
    private bool _addressRegisted = false;
    public bool AddressRegisted { get => _addressRegisted; set => _addressRegisted = value; }

    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // public async FTask Initialize()
    public void Initialize()
    {
        if (_inited)
            return;
        
        _inited = true;
        // _scene = await Fantasy.Entry.Initialize(GetType().Assembly);
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
    }

    private void OnConnectDisconnect()
    {
        Utils.Log("连接断开 Disconnect");
        OnDisconnected();
    }

    private void OnConnectFail()
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
    }

    private void OnConnectComplete()
    {
        Utils.Log("<color=yellow>连接成功 Complete</color>");
        // 每interval 2秒向服务器发送一次心跳，用于向服务器保活
        // 本地每 timeOutInterval 3秒检测 上次服务器回应是否超时， 超时时间为 timeOut 2秒
        // _session.AddComponent<SessionHeartbeatComponent>().Start(2000);
    }

    public bool IsConnected()
    {
        return false;
        // return _session is {IsDisposed: false};
    }
}
