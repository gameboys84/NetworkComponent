using System;
using System.Buffers;
using System.Collections.Generic;
using Google.Protobuf;
using Protocol;
using UnityEngine;

namespace TPFramework
{
    public class ProxyInfo
    {
        public string ip;
        public int port;
        public int retryTime;
    }

    public class NetMessageMgr
    {
        public static bool DebugLog = true;

        public delegate bool MessageCallback(int msgType, NetworkMessage content);

        public delegate void MessageHandler(NetworkMessage content);

        public delegate void NetFailedCallback();

        public class Data
        {
            public MessageCallback CallbackFunc;
            public bool IsWaiting;
            public bool WaitingShow = false;
            public float WaitingDelay = -1;
            public NetFailedCallback NetFailedCallback;
            public int MsgType;
            public float TimeOut = -1;
            public bool alreadyTimeOut = false;
        }

        public static readonly List<NetworkMessage> messageQueue = new List<NetworkMessage>();
        // public static readonly List<IMessage> messageQueue2 = new List<IMessage>();

        public const int HEARTBEATT_INTERVAL = 15000;

        public static float HeartbeatTimer = 0; // 心跳计时，剩余时间

        public static bool hasShowChoiceUI = false;
        // public static float TimeSyncTimer = 5 * NetMessageMgr.HEARTBEATT_INTERVAL;
        public static int QuickReconnCount = 0; // 快速重连次数
        private static bool WillReconnectLater = false; // 是否即将重连

        private static float ReconnectDelayTimer = 0; // 重连计时，当WillReconnectLater为true时，表示即将重连， 当计时>ReconnectDelayTimeMax时 开始进行重连

        private static int ReconnectDelayTimeMax = 0; // 每次尝试重连都会额外延时尝试的时间， 时间为 (RECONNECT_DELAY_TIME_BASE + QuickReconnCount * RECONNECT_DELAY_TIME_ACC)

        public static int pendingProxyIndx = 0; // 多个网关时，当前使用的网关索引
        private static bool ConnectingServer = false; // 是否正在连接网关
        private static float ConnectingServerTimer = 0.0f; // 连接网关计时，如果超出 CONNECTING_SERVERR_TIME_OUT 仍没连接成功，则表示连接失败

        public static bool IsLogin => isLogin;
        private static bool isLogin = false; // 是否已经登陆
        // private static bool isBootLoginDone = false; // 是否已经完成登录流程（可能是首次启动，或重连，或热重启到登录成功）

        private const int CONNECTING_SERVER_TIME_OUT = 5000; // 连接网关超时时间
        private const int RECONNECT_DELAY_TIME_BASE = 1000; // 重连延时基础时间
        private const int RECONNECT_DELAY_TIME_ACC = 500; // 重连延时递增时间
        
        private static float WaitingDelay = 0.2f; // 延时显示等待界面时间
        private static float TimeOut = 5f; // 消息发送超时时间
        private static int TimeOutCount = 0; // 超时的包计数，不能超过MaxPackTimeOutCount
        public static int MaxPackTimeOutCount = 3; // 最大发送包超时次数

        public static readonly List<Data> CallbackList = new List<Data>(); // 指定了手动回调处理的消息
        private static readonly List<NetFailedCallback> CallbacksWhenReconnect = new List<NetFailedCallback>();

        private static readonly Dictionary<int, MessageHandler> NetMsgHandles =
            new Dictionary<int, MessageHandler>(); // 预定义的消息处理方法

        public static List<ProxyInfo> proxy_list = new List<ProxyInfo>();
        public static Action onConnectCompleted, onConnectDisconnected, onConnectFailed;

        public enum ConnectCode
        {
            OK = 0,
            ConnectingTimeout, // 连接网关超时
            HeartBeatTimeout, // 心跳超时
            LoginTimeout, // 登陆超时
            SendFailed, // 消息发送失败
            ReadHeadDataError, // 读取包头数据错误
            ReadBodyDataError, // 读取包体数据错误
            ReadZeroData, // 读取到0字节数据
        }

        public enum HeartbeatStatus
        {
            Waiting, // 已发送心跳，等待服务器回应
            Received, // 已收到服务器回应，准备下一次发送
        }
        private static HeartbeatStatus heartbeatStatus = HeartbeatStatus.Received;

        // public enum LoginNTFType
        // {
        //     /// <summary>
        //     /// 游戏第一次启动并登陆完成收到NTF
        //     /// </summary>
        //     BootLogin,
        //
        //     /// <summary>
        //     /// 游戏通过reboot重启，比如切账号，中断超过1小时，登陆完成并收到NTF
        //     /// </summary>
        //     ReBootLogin,
        //
        //     /// <summary>
        //     /// 游戏中socket断线走QuickReconn流程， 登陆完成并收到NTF
        //     /// </summary>
        //     ReConnectLogin,
        //
        //     /// <summary>
        //     /// 游戏挂起一段时间，如设备中断，但并未断线，会请求重要的NTF重发
        //     /// </summary>
        //     GamePauseLogin,
        // }

        // public delegate void OnQuickReconnectBack(NetMessageMgr.LoginNTFType type);
        // public static event OnQuickReconnectBack onQuickReconnectBack;

        public static void OnConnectFailed(ConnectCode codeId, string msg = "")
        {
            if (hasShowChoiceUI || ConnectingServer || WillReconnectLater || proxy_list.Count == 0)
                return;
            
            DLog.Error("OnConnectFailed code:{0}, msg:{1}, ConnectingServer:{2}, WillReconnectLater:{3}, hasShowChoiceUI:{4}, proxyIndex:{5}, proxy_list.Count:{6}, QuickReconnCount:{7}, ConnectingTimer:{8}, TimeStartup:{9}", 
                codeId, msg, ConnectingServer, WillReconnectLater, hasShowChoiceUI, pendingProxyIndx, proxy_list.Count, QuickReconnCount, ConnectingServerTimer, Time.realtimeSinceStartup);

            if (NetManager.GetSession().IsConnected())
            {
                NetManager.GetSession().Close();
            }

            isLogin = false;
            if (QuickReconnCount >= proxy_list[pendingProxyIndx].retryTime)
            {
                ConnectingServerTimer = 0;
                QuickReconnCount = 0;
                pendingProxyIndx++;
                if (pendingProxyIndx >= proxy_list.Count)
                {
                    pendingProxyIndx = 0;
                    hasShowChoiceUI = true;
                    
                    //TODO: 重连失败，用户确认后重启游戏
                    onConnectFailed?.Invoke();
                    // Action actionRestart = () =>
                    // {
                    //     Reset();
                    //     Boot.Reboot();
                    // };
                    //
                    // actionRestart();
                    return;
                }
            }
            
            // 稍微延迟一下了再重连
            WillReconnectLater = true;
            ReconnectDelayTimer = 0;
            ReconnectDelayTimeMax = RECONNECT_DELAY_TIME_BASE + QuickReconnCount * RECONNECT_DELAY_TIME_ACC;
        }

        public static void Reset()
        {
            if (NetManager.GetSession().IsConnected() || ConnectingServer)
            {
                NetManager.GetSession().Close();
                ConnectingServer = false;
            }

            CallbackList.Clear();
            CallbacksWhenReconnect.Clear();
            NetMsgHandles.Clear();
            proxy_list.Clear();
            // messageQueue.Clear();

            pendingProxyIndx = 0;
            isLogin = false;
            // isBootLoginDone = false;
            hasShowChoiceUI = false;
            WillReconnectLater = false;
            ReconnectDelayTimer = 0;
            ReconnectDelayTimeMax = 0;
            QuickReconnCount = 0;
            HeartbeatTimer = 0;
            // TimeSyncTimer = 5 * NetMessageMgr.HEARTBEATT_INTERVAL;

        }

        public static void Init()
        {
            ArrayPool<byte>.Create(65536, 16);
            NetReachability.OnNetReachabilityChange += OnNetReachabilityChange;
        }

        public static void InitNet(List<ProxyInfo> proxyList)
        {
            messageQueue.Clear();

            for (int i = 0; i < proxyList.Count; i++)
            {
                proxy_list.Add(new ProxyInfo()
                {
                    ip = proxyList[i].ip,
                    port = proxyList[i].port,
                    retryTime = proxyList[i].retryTime
                });
            }
            
            ConnectServer();
        }

        private static void OnNetReachabilityChange(bool reachable)
        {
            if (reachable)
                return;

            hasShowChoiceUI = true;

            //TODO: 显示网络连接失败的提示，待用户确认重连后重新连接
            onConnectFailed?.Invoke();

            // Action actionReconnect = () =>
            // {
            //     // hasShowChoiceUI = false;
            //     WillReconnectLater = ConnectingServer = hasShowChoiceUI = false;
            //     ReconnectDelayTimer = ConnectingServerTimer = QuickReconnCount = pendingProxyIndx = 0;
            //
            //     if (proxy_list.Count == 0)
            //     {
            //         // 还没获取到网关，直接重启游戏
            //         Reset();
            //         Boot.Reboot();
            //         return;
            //     }
            //
            //     QuickReconnect();
            //     NetReachability.Reset();
            // };

            // 点确认后的操作
            // actionReconnect();
        }

        public static void QuickReconnect()
        {
            DLog.Warning("QuickReconn =========== {0} {1} {2} {3}", JsonUtility.ToJson(proxy_list[pendingProxyIndx]),
                NetManager.GetSession().IsConnected(), pendingProxyIndx, QuickReconnCount);
            
            NetManager.GetSession().Close();
            
            onConnectDisconnected?.Invoke();
            
            ConnectingServer = true;
            ConnectingServerTimer = 0;
            HeartbeatTimer = 0;

            foreach (Data callback in CallbackList)
            {
                if (callback != null && callback.NetFailedCallback != null)
                {
                    CallbacksWhenReconnect.Add(callback.NetFailedCallback);
                }
            }
            CallbackList.Clear();
            
            QuickReconnCount++;
            
            // 开始连接网关
            ProxyInfo proxy = proxy_list[pendingProxyIndx];
            NetManager.GetSession().Connect(proxy.ip, proxy.port);
        }

        public static void ConnectServer()
        {
            pendingProxyIndx = 0;
            if (proxy_list == null || proxy_list.Count == 0 || pendingProxyIndx >= proxy_list.Count)
            {
                DLog.Error($"proxy_list is null or empty, {proxy_list?.Count}");
                return;
            }
            
            hasShowChoiceUI = false;
            ConnectingServer = true;
            ConnectingServerTimer = 0;
            
            QuickReconnCount = 0;
            WillReconnectLater = false;
            ReconnectDelayTimer = 0;
            
            ProxyInfo proxy = proxy_list[pendingProxyIndx];
            DLog.Log("ConnectServer {0}:{1}", proxy.ip, proxy.port);
            NetManager.GetSession().Connect(proxy.ip, proxy.port);
        }

        public static void RegisterMsgHandle(int msgType, MessageHandler handle)
        {
            NetMsgHandles[msgType] = handle;
        }
        
        public static void UnRegisterMsgHandle(int msgType)
        {
            NetMsgHandles.Remove(msgType);
        }
        
        public static void OnConnectSuccess()
        {
            isLogin = false;
            // 标记连接成功
            ConnectingServer = false;
            
            onConnectCompleted?.Invoke();

            SendCheckConnection();
            
            ResetHeartBeatTimer();
            heartbeatStatus = HeartbeatStatus.Received;
        }

        public static void SendCheckConnection()
        {
            DLog.Log("SendCheckConnection START");
            var msg = new CheckConnectionClient2Gate();
            SendMsg(MsgType.CheckConnectionReq, msg, (type, content) =>
            {
                DLog.Log($"CheckConnectionReq: {type}, {content.GetType()}");
                
                var msg = CheckConnectionGate2Client.Parser.ParseFrom(content.Data);
                if (!msg.Successful)
                {
                    DLog.Error("CheckConnectionReq failed");
                    return true;
                }
                
                DLog.Log("CheckConnectionReq successful, and will PlayerLoginClient2Gate");
                
                return true;
            }, true, null);
        }

        public static void SendMsg(int msgType, IMessage content, MessageCallback callback,
            bool isWaiting, NetFailedCallback netFailedCallback)
        {
            if (msgType != MsgType.HeartBeatReqAck && content != null)
            {
                CheckSendRecvTime();
            }
            
            if (ConnectingServer)
                return;

            var session = NetManager.GetSession();
            if (session == null || !session.IsConnected())
                return;
            
            bool isOK = session.Send((int)msgType, content);
            if (!isOK)
            {
                OnConnectFailed(ConnectCode.SendFailed, "SendMsg failed");
                return;
            }

            // if (callback != null)
            // {
                Data data = new Data();
                data.CallbackFunc = callback;
                data.IsWaiting = isWaiting;
                data.NetFailedCallback = netFailedCallback;
                data.MsgType = (int)msgType;
                data.WaitingShow = false;
                if (isWaiting)
                    data.WaitingDelay = Time.realtimeSinceStartup + WaitingDelay;
                data.TimeOut = Time.realtimeSinceStartup + TimeOut;
                CallbackList.Add(data);
            // }
        }

        public static void DispatchMessage(int msgType, NetworkMessage content)
        {
            lastRecvTime = TimeUtils.GetCurServerTimeSec();

            int msgIdx = -1;
            for (int i = 0; i < CallbackList.Count; i++)
            {
                Data data = CallbackList[i];
                if (data.MsgType != msgType)
                    continue;

                if (data.CallbackFunc == null)
                    msgIdx = i;
                else
                {
                    bool isHandled = data.CallbackFunc((int) msgType, content);
                    if (isHandled)
                    {
                        ResetHeartBeatTimer();
                        // float waitDur = Time.realtimeSinceStartup + TimeOut - data.TimeOut;
                        if (data.IsWaiting && data.WaitingShow)
                        {
                            // 显示转圈的界面
                            //TODO: 显示转圈的界面
                        }

                        CallbackList.RemoveAt(i);
                        TimeOutCount = 0;

                        // 设置了直接回调的消息，只有理一个待处理，完成后返回即可
                        return;
                    }
                }
            }
            
            // 还有可能是注册的消息，则查找注册的消息
            if (!NetMsgHandles.TryGetValue(msgType, out var msgHandle))
            {
                DLog.Error("no msgHandler found for msgType:{0}", msgType);
                return;
            }
            
            msgHandle(content);
            if (msgIdx >= 0)
            {
                CallbackList.RemoveAt(msgIdx);
                TimeOutCount = 0;
            }
        }

        public static void Update(int dt)
        {
            if (WillReconnectLater)
            {
                ReconnectDelayTimer += dt;
                if (ReconnectDelayTimer > ReconnectDelayTimeMax)
                {
                    WillReconnectLater = false;
                    ReconnectDelayTimer = 0;
                    QuickReconnect();
                }
            }
            else if (ConnectingServer)
            {
                ConnectingServerTimer += dt;
                if (ConnectingServerTimer > CONNECTING_SERVER_TIME_OUT)
                {
                    ConnectingServer = false;
                    OnConnectFailed(ConnectCode.ConnectingTimeout, "connect time out");
                }
            }
            
            
            while (messageQueue.Count != 0)
            {
                NetworkMessage msg = messageQueue[0];
                messageQueue.RemoveAt(0);
                DispatchMessage(msg.GetMsgType(), msg);
            }

            // 判断是否有 waiting 的消息
            CheckWaitMessage();
            
            if (HeartbeatTimer > 0)
            {
                HeartbeatTimer -= dt;
                CheckTimeOutMessage();
                if (HeartbeatTimer <= 0 && isLogin)
                {
                    ResetHeartBeatTimer();
                    if (heartbeatStatus == HeartbeatStatus.Received)
                        SendHeartBeat();
                    else
                        OnConnectFailed(ConnectCode.HeartBeatTimeout, "heart beat time out");

                }
                
                // 同步服务器时间
                // TimeSyncTimer -= dt;
                // if (TimeSyncTimer < 0 && isLogin)
                // {
                //     TimeSyncTimer = 5 * HEARTBEATT_INTERVAL;
                //     GameGlobalData.SyncTimeReq();
                // }
            }
        }

        private static void ResetHeartBeatTimer()
        {
            HeartbeatTimer = HEARTBEATT_INTERVAL;
        }

        // 以下2种情况时，会加快下次心跳的时间间隔
        // 1. 当上次收到服务器消息超过5秒， 且上次发消息过了1-5秒
        // 2. 如果多次消息超时(>=3)，即发消息后未收到服务器的回应（可能服务器报错，而不是网络问题）， 只要收到一次服务的回应，就会重置超时次数
        private static void FastTestServerByHeartBeat()
        {
            if (!isLogin && HeartbeatTimer > 0)
                return;

            SendHeartBeat();
            HeartbeatTimer = 2000;
        }

        private static void SendHeartBeat()
        {
            heartbeatStatus = HeartbeatStatus.Waiting;

            // SendMsg(Protocol.MsgType.HeartBeat, new NetworkMessage());
        }

        private static long lastSendTime = 0;
        private static long lastRecvTime = 0;
        private static void CheckSendRecvTime()
        {
            long now = TimeUtils.GetCurServerTimeSec();
            long deltaSendTime = now - lastSendTime;
            long deltaRecvTime = now - lastRecvTime;
            if (deltaRecvTime > 5 && deltaSendTime < 5 && deltaSendTime > 1)
            {
                DLog.Warning("CheckSendRecvTime maybe lost connection, deltaSendTime:{0}, deltaRecvTime:{1}", deltaSendTime, deltaRecvTime);
                lastSendTime = lastRecvTime = now;
                FastTestServerByHeartBeat();
            }
            else
            {
                lastSendTime = now;
            }
        }
        
        private static void CheckWaitMessage()
        {
            float GameNow = Time.realtimeSinceStartup;

            for (int i = 0; i < CallbackList.Count; i++)
            {
                Data data = CallbackList[i];
                if(data.IsWaiting && !data.WaitingShow && GameNow> data.WaitingDelay)
                {
                    // 显示转圈的界面
                    // UGameWaiting.GetInstance().GameWaiting_ShowLoadingScreen();
                    data.WaitingShow = true;
                }
            }
        }
        
        static List<int> remove = new List<int>(); // 超时将要移除的消息索引
        private static void CheckTimeOutMessage()
        {
            if (WillReconnectLater)
                return;
            remove.Clear();
            float GameNow = Time.realtimeSinceStartup;
            for (int i = 0; i < CallbackList.Count; i++)
            {
                Data v = CallbackList[i];
                if (v.TimeOut >= 0 && v.TimeOut < GameNow)
                {
                    if (v.IsWaiting && v.WaitingShow)
                    {
                        // 消息超时了，关闭转圈的界面
                        // UGameWaiting.GetInstance().GameWaiting_HideLoadingScreen();
                    }

                    TimeOutCount += 1;
                    DLog.Error("Warning!!! timeout message, {0} {1}/{2}", v.MsgType, TimeOutCount, MaxPackTimeOutCount);

                    v.TimeOut = GameNow + TimeOut;
                    if (v.alreadyTimeOut)
                    {
                        if (v.MsgType == (int)MsgType.PlayerLoginReq)
                        {
                            OnConnectFailed(ConnectCode.LoginTimeout, "login timeout");
                            return;
                        }
                        
                        // 将超时的消息移出待处理列表
                        remove.Add(i);
                    }
                    else
                    {
                        v.alreadyTimeOut = true;
                    }
                    
                    // 如果有多个消息超时，就发送一次心跳, 如果多次心跳没回应，就会提示断开连接， 但如果心跳正常，只是某些消息没回应，逻辑上其实是忽略了这个错误
                    if (TimeOutCount >= MaxPackTimeOutCount)
                    {
                        TimeOutCount = 0;
                        FastTestServerByHeartBeat();
                        break;
                    }
                }
            }
            for (int i = remove.Count - 1; i >= 0; i--)
                CallbackList.RemoveAt(remove[i]);
        }
    }
}