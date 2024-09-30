using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace TPFramework
{
    class StateObject {
        public byte[] m_mainBuffer;
        public byte[] m_buffer;
        public int m_needbytes;
        public NetSession.RawdataHandler m_handler;
        public Socket m_socket;
        public bool isBigPack;
    }
    
    public class NetSession : ISession
    {
        private const int BUFFER_SIZE = 128;
        private const int HEADER_SIZE = 4; // 4 bytes for size
        private const int NOBODY_BUFFER_SIZE = 8; // 8 bytes for size + msgType
        private static int MAX_PACKET_PER_FRAME = 15;
        private Socket m_socket;
        Action m_disconnectionHandler = null; // 断开连接事件回调
        Action m_connectSuccessHandler = null; // 连接成功事件回调
        
        readonly Queue<Action> m_tasks = new Queue<Action>();

	    readonly List<byte[]> m_sendQueue = new List<byte[]>();
	    private readonly object _locker = new object();
        private bool m_isSending = false;
        
        // 消息处理
        private readonly Dictionary<Protocol.MsgType, NetworkMessage.MessageHandlerType> m_msgHandlers = new Dictionary<Protocol.MsgType, NetworkMessage.MessageHandlerType>();
        private NetworkMessage.MessageHandlerType m_defaultMsgHandler;
        
        Pike m_decrypt = null;
        Pike m_encrypt = null;
        // private Pike m_piketest;
        
        private System.Threading.Thread readThread = null;                               //读线程
        // private System.Threading.Thread writeThread = null;                              //写线程
        private bool colseThread = true;                                //是否关闭线程
        private static object lockObj = new object();                   //类锁
        
        public delegate void RawdataHandler(byte[] buf);

        #region public interface


        #endregion

        #region Implementation of ISession

        public void Connect(string host, int port)
        {
            DLog.Log("Try to connect:" + host + " " + port);
            if (IsConnected()) {
                Close();
                return;
            }
            // TrackMgr.ClientNetConnectTrack(host + ":" + port, "socket_connect_begin", NetMessageMgr.getTrackInfo(""));
            Reset();
            try {
                //IPHostEntry lipa = Dns.GetHostEntry(host);
                int? tmpPort = port;

                // m_connetUrl = host;
                // m_connectPort = port;
                // m_ip_port = m_connetUrl + ":" + m_connectPort;

                Dns.BeginGetHostAddresses(host, asyncResult => {
                    QueueOnMainThread(() => {
                        GetHostEntryCallbackMainThread(asyncResult);
                    });
                }, tmpPort);
            } catch (Exception e) {
                DLog.Error(e.ToString());
                Close();
            }
        }

        public void Register(Protocol.MsgType msgType, NetworkMessage.MessageHandlerType handler)
        {
            m_msgHandlers[msgType] = handler;
        }

        public void Register(NetworkMessage.MessageHandlerType handler)
        {
            m_defaultMsgHandler = handler;
        }

        public bool Send(Protocol.MsgType msgType, NetworkMessage msg)
        {
            if (!IsConnected())
                return false;
            
            // 序列化消息
            if (msg != null)
            {
                // 有消息体的消息
                SPack sp = new SPack();
                sp.offset = HEADER_SIZE;
                sp.buf = new byte[BUFFER_SIZE];
                
                sp.Write((int)msgType);
                msg.Encode(sp);

                int size = (sp.offset - HEADER_SIZE); // realSize
                // if (size < UInt32.MaxValue)
                {
                    // max 2^32 bytes length data
                    sp.offset = 0;
                    sp.Write(size);
                    Array.Resize(ref sp.buf, size + HEADER_SIZE);
                }
                // else
                // {
                //     // max 2^31 bytes length data
                //     sp.offset = 0;
                //     sp.Write((UInt16) 0xFFFF); // 前2字节固定写 0xFFFF
                //     sp.Write(size); // 然后接实际大小
                // }
                
                if (m_encrypt != null) {
                    m_encrypt.Codec(sp.buf, sp.offset, size);
                }
                Send(sp.buf);
            }
            else
            {
                // 无消息体的消息
                byte[] buf = new byte[NOBODY_BUFFER_SIZE];
                int offset = 0;
                NetworkSerialization.Write(ref buf, ref offset, (NOBODY_BUFFER_SIZE-HEADER_SIZE));
                NetworkSerialization.Write(ref buf, ref offset, (int)msgType);
                if (m_encrypt != null) {
                    m_encrypt.Codec(buf, HEADER_SIZE, buf.Length - HEADER_SIZE);
                }
                Send(buf);
            }
            
            return true;
        }

        public void RegisterDisconnectionHandler(Action handler)
        {
            m_disconnectionHandler = handler;
        }

        public void RegisterConnectSuccessHandler(Action handler)
        {
            m_connectSuccessHandler = handler;
        }

        public void UpdateSession()
        {
            throw new NotImplementedException();
        }

        public void Close(bool force = false)
        {
            throw new NotImplementedException();
        }

        public bool IsConnected()
        {
            return m_socket != null && m_socket.Connected;
        }

        #endregion
        
        byte[] Combine(byte[] b1, byte[] b2, int size)
        {
            if (b1 == null) {
                byte[] ret = new byte[size];
                Array.Copy(b2, ret, size);
                return ret;
            }
            byte[] combined = new byte[b1.Length + size];
            Array.Copy(b1, combined, b1.Length);
            Array.Copy(b2, 0, combined, b1.Length, size);
            return combined;
        }
        
        void Reset()
        {
            ClearTaskQueue();
            lock (_locker) {
			    if (m_sendQueue.Count>0)
				    DLog.Error("ei] sendQueue size ["+m_sendQueue.Count+"], isSending "+m_isSending);
                m_sendQueue.Clear();
			    m_isSending = false;
                DLog.Log("socket reset");
            }

            m_decrypt = null;
            m_encrypt = null;
        }

        void Send(byte[] buf)
        {
            if (!IsConnected()) {
                return;
            }

		    lock (_locker) {
			    m_sendQueue.Add(buf);
			    if (!m_isSending) {
				    m_isSending = true;
				    byte[] sbuf = m_sendQueue[0];
				    try {
                        // sendInstanceTime = ext.getSysTime();
                        //if(NetMessageMgr.DebugLog)
                        //    DLog.Log("send msg at {0} {1}", sendInstanceTime, m_sendQueue.Count);
                        m_socket.BeginSend(sbuf, 0, sbuf.Length, 0, SendCallback, m_socket);
				    } catch (SocketException e) {
					    DLog.Log(e.ToString());
					    Close();
				    }
			    }
		    }
        }
        
        void SendCallback(IAsyncResult ar)
        {
            Socket sock = (Socket)ar.AsyncState;
            if (!sock.Connected) {
                return;
            }
            try {
                lock (_locker) {
                    int send = sock.EndSend(ar);
                    if (send == m_sendQueue[0].Length) {
                        m_sendQueue.RemoveAt(0);
                    } else if (send < m_sendQueue[0].Length) {
                        int newBufSize = m_sendQueue[0].Length - send;
                        byte[] newBuf = new byte[newBufSize];
                        Array.Copy(m_sendQueue[0], send, newBuf, 0, newBufSize);
                        m_sendQueue[0] = newBuf;
                    }

                    if (m_sendQueue.Count > 0) {
                        byte[] buf = m_sendQueue[0];
                        m_socket.BeginSend(buf, 0, buf.Length, 0, SendCallback, m_socket);
                    } else {
                        m_isSending = false;
                    }
                }
            } catch (SocketException e) {
                if (NetMessageMgr.DebugLog)
                {
                    string str1 = e.ToString();
                    QueueOnMainThread(() => {
                        DLog.Log(str1);
                    });
                }
                QueueOnMainThread(() => {
                    Close();
                });

            }
        }
    
        // bool packetAccumulation = false;
        // int startFrame;
        // float startTime;
        // int countMax = 0;
        // void reportPacketCount(int count)
        // {
        //     if (NetMessageMgr.DebugLog)
        //         if (count > MAX_PACKET_PER_FRAME)
        //         {
        //             DLog.Warning(Time.frameCount + " reack max packet " + m_tasks.Count);
        //         }
        //     if (count > MAX_PACKET_PER_FRAME && packetAccumulation == false)
        //     {
        //         startFrame = Time.frameCount;
        //         startTime = Time.realtimeSinceStartup;
        //         packetAccumulation = true;
        //     }
        //     else if(count <= MAX_PACKET_PER_FRAME && count > 0 && packetAccumulation == true)
        //     {
        //         // TrackMgr.ClientNetConnectTrack(NetMessageMgr.GetEndPoint(), "packetAcc", NetMessageMgr.getTrackInfo(string.Format("{0} {1} {2} {3}", countMax, startFrame, Time.frameCount - startFrame, Time.realtimeSinceStartup - startTime)));
        //         packetAccumulation = false;
        //         countMax = 0;
        //     }
        //     if(packetAccumulation && count > countMax)
        //     {
        //         countMax = count;
        //     }
        // }
        
        void HandleTasks()
        {
            int taskHandleCount = 0;
            // reportPacketCount(m_tasks.Count);
            int muti = 3; //MainUI.Instance == null ? 3: 1;
            while (m_tasks.Count > 0 && taskHandleCount < muti * MAX_PACKET_PER_FRAME) {
                taskHandleCount++;
                Action task = null;

                lock (m_tasks) {
                    if (m_tasks.Count > 0) {
                        task = m_tasks.Dequeue();
                    }
                }

                task();
            }
        }

        void QueueOnMainThread(Action task)
        {
            lock (m_tasks) {
                m_tasks.Enqueue(task);
            }
        }

        void ClearTaskQueue()
        {
            lock (m_tasks) {
                m_tasks.Clear();
            }
        }
        
        void GetHostEntryCallbackMainThread(IAsyncResult asyncResult)
        {
            int? port = asyncResult.AsyncState as int?;
            IPAddress[] ipAddr = Dns.EndGetHostAddresses(asyncResult);
            if (ipAddr != null)
            {
                List<IPAddress> addrList = new List<IPAddress>(ipAddr);
                DoConnect(addrList, port.Value);
            }
            else
            {
                DLog.Error("DNS error: can't parse address.");
            }
        }
        
        bool ConnectCallBackMainThread(IAsyncResult asyncResult)
        {
            Socket sock = (Socket)asyncResult.AsyncState;
            try {
                sock.EndConnect(asyncResult);
            } catch (Exception e) {
                if (NetMessageMgr.DebugLog)
                    DLog.Log(e.ToString());
                //Close();
                return false;
            }
            if (m_connectSuccessHandler != null) {
                m_connectSuccessHandler();
            }

            // DoRead();
            //创建读写线程
            CreateThreads();
            return true;
        }

        void ReadBodyCallback(byte[] buf)
        {
            if (buf == null) {
                QueueOnMainThread(() => {
                    NetMessageMgr.OnConnectFailed(NetMessageMgr.ConnectCode.ReadDataError, "read error");
                });
                return;
            }
            if (m_decrypt != null) {
                m_decrypt.Codec(buf, 0, buf.Length);
            }

            QueueOnMainThread(() => {
                HandleRawdata(buf);
            });

        }

        void HandleRawdata(byte[] buf)
        {
            int offset = 0;

            int type = NetworkSerialization.ReadUInt16(buf, ref offset);
            Protocol.MsgType msgType = (Protocol.MsgType)type;
            //TODO:
            
            // if (Protocol.MsgType.SESSION_TOKEN_NTF == msgType) {
            //     InitPike(buf, offset);
            //     HandleMsg(buf, offset, msgType, nid);
            // } else if (Protocol.MsgType.COMPRESSED_NTF == msgType) {
            //     HandleCompressedMsg(buf, offset);
            // } else if (Protocol.MsgType.COMPRESSED4B_NTF == msgType){
            //     Handle4ByteCompressedMsg(buf, offset);
            // } else if (m_defaultMsgHandler != null || m_msgHandlers.ContainsKey(msgType)) {
            //     HandleMsg(buf, offset, msgType, nid);
            //     //if (nid == 0)
            //     //    DLog.Error($"msg[{msgType}] nid = 0");
            // }
        }

        private int InitPike(byte[] buf, int offset)
        {
            UInt32 key1 = NetworkSerialization.ReadUInt32(buf, ref offset);
            UInt32 key2 = NetworkSerialization.ReadUInt32(buf, ref offset);

            if (key1 == 0 && key2 == 0) {
                return offset;
            }

            var token = ((Int64)(key1) << 32) | (Int64)(key2);
            var u1 = (UInt32)(token >> 3) & 0xFF;
            var u2 = (UInt32)(token >> 23) & 0xFF;
            var u3 = (UInt32)(token >> 37) & 0xFF;
            var u4 = (UInt32)(token >> 47) & 0xFF;
            var key = (u1 << 24) | (u3 << 16) | (u2 << 8) | u4;

            m_decrypt = new Pike(key);
            m_encrypt = new Pike(key);
            // m_piketest = new Pike(key);
            return offset;
        }

        // private int HandleCompressedMsg(byte[] buf, int offset)
        // {
        //     int origSize = NetworkSerialization.ReadInt32(buf, ref offset);
        //     UInt16 compSize = NetworkSerialization.ReadUInt16(buf, ref offset);
        //     byte[] origData = LZ4ps.LZ4Codec.Decode64(buf, offset, compSize, origSize);
        //     int origOffset = 0;
        //     UInt16 origType = NetworkSerialization.ReadUInt16(origData, ref origOffset);
        //     HandleRawdata(origData);
        //     return offset;
        // }
        //
        // private int Handle4ByteCompressedMsg(byte[] buf, int offset)
        // {
        //     int origSize = NetworkSerialization.ReadInt32(buf, ref offset);
        //     int compSize = NetworkSerialization.ReadInt32(buf, ref offset);
        //     byte[] origData = LZ4ps.LZ4Codec.Decode64(buf, offset, compSize, origSize);
        //     int origOffset = 0;
        //     UInt16 origType = NetworkSerialization.ReadUInt16(origData, ref origOffset);
        //     HandleRawdata(origData);
        //     return offset;
        // }

        void DoConnect(List<IPAddress> addrList, int port)
        {
            if (addrList.Count == 0) {
                DLog.Log("Failed to connect the gate entry!");
                Close();
                return;
            }
            IPAddress addr = addrList[0];
            DLog.Log("Try to connect entry {0} {1}", addr, addr.AddressFamily);
            lock (lockObj) //socket保护
            {
                IPEndPoint ipe = new IPEndPoint(addr, port);

                // m_connectIp = addr.ToString();

                m_socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
                m_socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                LingerOption myOpts = new LingerOption(true, 0);
                m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, myOpts);
                //m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                //m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout,3000);
                //m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout,3000);

                m_socket.BeginConnect(ipe, asyncResult =>
                {
                    QueueOnMainThread(() =>
                    {
                        if (!ConnectCallBackMainThread(asyncResult))
                        {
                            addrList.RemoveAt(0);
                            DoConnect(addrList, port); //ei:if addrList null, need tips check network or retry by hand
                        }
                    });
                }, m_socket);
            }
        }
        
        private void CreateThreads()
        {
            lock(lockObj)
            {
                if (this.IsConnected())
                {
                    this.colseThread = false;
                    this.readThread = new System.Threading.Thread(this.ReadData);   //接收使用独立线程
                    this.readThread.IsBackground = true;
                    this.readThread.Start(m_socket);

                    // this.writeThread = new Thread(this.writeData);  //写数据线程
                    // this.writeThread.IsBackground = true;
                    // this.writeThread.Start(this.m_socket);
                    //
                    // RecordLog("启动读写数据线程  线程ID = " + Thread.CurrentThread.ManagedThreadId.ToString());
                }   
            }
        }
        
        private void ReadData(object o)
        {
            DLog.Log("创建读取数据  线程ID = " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
            
            Socket client = o as Socket;
            var stateObj = new StateObject();
            //先读头获取协议体大小
            stateObj.m_needbytes = HEADER_SIZE;
            int read = 0;
            
            while (true)
            {
                stateObj.m_buffer = new byte[stateObj.m_needbytes];

                try
                {
                    // m_socket.BeginReceive(so.m_buffer, 0, so.m_needbytes, 0, ReadCallback, so);
                    read = client.Receive(stateObj.m_buffer, 0, stateObj.m_needbytes, 0);
                    lock (lockObj)
                    {
                        if (colseThread)
                        {
                            DLog.Warning("readData 退出线程");
                            break;
                        }

                        if (client != m_socket)
                        {
                            DLog.Warning("readData socket 已经更换 退出线程 : " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                            break;
                        }
                        
                        if (read > 0)
                        {
                            stateObj.m_mainBuffer = Combine(stateObj.m_mainBuffer, stateObj.m_buffer, read);
                            stateObj.m_needbytes -= read;
                            if (stateObj.m_needbytes == 0)
                            {
                                int offset = 0;
                                int size = stateObj.isBigPack
                                    ? NetworkSerialization.ReadInt32(stateObj.m_mainBuffer, ref offset)
                                    : NetworkSerialization.ReadUInt16(stateObj.m_mainBuffer, ref offset);
                                if(size == 0xFFFF && !stateObj.isBigPack)
                                {
                                    DLog.Warning("recv a big packet, use 4 bytes header !!");
                                    // Read(4, Read4ByteHeaderCallback);
                                    stateObj.m_needbytes = 4;
                                    stateObj.m_mainBuffer = null;
                                    stateObj.isBigPack = true;
                                }
                                else
                                {
                                    var stateBodyObj = new StateObject
                                    {
                                        m_socket = client,
                                        m_needbytes = size
                                    };

                                    if (!ReadBodyData(stateBodyObj)) break;
                            
                                    stateObj.m_needbytes = HEADER_SIZE;
                                    stateObj.m_mainBuffer = null;
                                    stateObj.isBigPack = false;
                                }
                            }
                        }
                        else
                        {
                            if (NetMessageMgr.DebugLog)
                                DLog.Error("ReadCallback: " + read + " IsConnected:" + IsConnected());
                            QueueOnMainThread(() => {
                                NetMessageMgr.OnConnectFailed(NetMessageMgr.ConnectCode.ReadDataError, "read error");
                            });
                            break;
                        }
                    }
                }
                catch (SocketException e)
                {
                    if (NetMessageMgr.DebugLog)
                        DLog.Log(e.ToString());
                    break;
                }
                catch (Exception e)
                {
                    if (NetMessageMgr.DebugLog)
                        DLog.Log(e.ToString());
                    break;
                }
            }
        }
        
        private bool ReadBodyData(StateObject stateObj)
        {
            int read = 0;
            while (true)
            {
                stateObj.m_buffer = new byte[stateObj.m_needbytes];
                try
                {
                    read = stateObj.m_socket.Receive(stateObj.m_buffer, 0, stateObj.m_needbytes, 0);
                    
                    if (read > 0)
                    {
                        stateObj.m_mainBuffer = Combine(stateObj.m_mainBuffer, stateObj.m_buffer, read);
                        stateObj.m_needbytes -= read;
                        if (stateObj.m_needbytes == 0)
                        {
                            ReadBodyCallback(stateObj.m_mainBuffer);
                        
                            if (colseThread)
                            {
                                DLog.Warning("readData 退出线程");
                                return false;
                            }

                            if (stateObj.m_socket != m_socket)
                            {
                                DLog.Warning("readData socket 已经更换 退出线程");
                                return false;
                            }
                        
                            return true;
                        }
                    }
                    else
                    {
                        if (NetMessageMgr.DebugLog)
                            DLog.Error("ReadCallback: " + read + " IsConnected:" + IsConnected());
                        ReadBodyCallback(null);
                        return false;
                    }
                }
                catch (SocketException e)
                {
                    if (NetMessageMgr.DebugLog)
                        DLog.Log(e.ToString());
                    return false;
                }
                catch (Exception e)
                {
                    if (NetMessageMgr.DebugLog)
                        DLog.Log(e.ToString());
                    return false;
                }
            }
        }
    }
}