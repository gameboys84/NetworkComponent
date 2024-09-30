
using System.Collections.Generic;
using TPFramework;
using UnityEngine;

namespace TPFramework
{
    public class NetManager : MonoBehaviour
    {
        readonly static Dictionary<int,ISession> sessionList = new Dictionary<int, ISession>();

        public static void InitSession(int key, ISession session)
        {
            sessionList[key] = session;
            
            session.RegisterConnectSuccessHandler(() =>
            {
                DLog.Log("Connect success");
                // 连接服务器成功， 可以开启心跳
                NetMessageMgr.OnConnectSuccess();
            });
        }
        
        public static ISession GetSession(int key = 0)
        {
            if (sessionList.ContainsKey(key)) {
                return sessionList[key];
            } else {
                ISession newSession = new NetSession();
                InitSession(key, newSession);
                return newSession;
            }
        }
        
        void Start ()
        {
            gameObject.AddComponent<NetReachability>();
            DontDestroyOnLoad(gameObject);
        }

        void Update () {
            foreach (var session in sessionList) {
                session.Value.UpdateSession();
            }
            
            // 也可以放在统一的地方来驱动，以保证更新顺序
            int dt = (int)(Time.deltaTime * 1000);
            NetMessageMgr.Update(dt);
        }

        private void OnDestroy()
        {
            foreach (ISession session in sessionList.Values)
            {
                session.RegisterDisconnectionHandler(null);
                session.Close();
            }

            sessionList.Clear();
            CPool.Reset();
            Protocol.RunVer = Protocol.Ver;
        }
    }
}