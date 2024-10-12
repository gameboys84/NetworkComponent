using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Protocol;

namespace TPFramework
{
    class StateObject {
        public byte[] m_mainBuffer;
        public byte[] m_buffer;
        public int m_needbytes;
        public int m_totalbytes;
        public NetSession.RawdataHandler m_handler;
        public Socket m_socket;
        // public bool isBigPack;

        public string m;
        public bool header; // 是否解析包头或包体
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

	    // readonly List<byte[]> m_sendQueue = new List<byte[]>();
	    // private readonly object _locker = new object();
        // private bool m_isSending = false;
        
        // 消息处理
        private readonly Dictionary<int, NetworkMessage.MessageHandlerType> m_msgHandlers = new Dictionary<int, NetworkMessage.MessageHandlerType>();
        private NetworkMessage.MessageHandlerType m_defaultMsgHandler;
        
        // Pike m_decrypt = null;
        // Pike m_encrypt = null;
        // private Pike m_piketest;
        
        // private System.Threading.Thread readThread = null;                               //读线程
        // private System.Threading.Thread writeThread = null;                              //写线程
        // private bool closeThread = true;                                //是否关闭线程
        // private static object lockObj = new object();                   //类锁
        
        public delegate void RawdataHandler(byte[] buf, int size);

        #region public interface


        #endregion

        #region Implementation of ISession

        public void Connect(string host, int port)
        {
            DLog.Log("[NetSession] Try to connect:" + host + " " + port);
            if (IsConnected()) {
                Close();
                return;
            }
            // TrackMgr.ClientNetConnectTrack(host + ":" + port, "socket_connect_begin", NetMessageMgr.getTrackInfo(""));
            Reset();
            try {
                //IPHostEntry lipa = Dns.GetHostEntry(host);
                int tmpPort = port;

                // m_connetUrl = host;
                // m_connectPort = port;
                // m_ip_port = m_connetUrl + ":" + m_connectPort;

                Dns.BeginGetHostAddresses(host, asyncResult => {
                    QueueOnMainThread(() => {
                        GetHostEntryCallbackMainThread(asyncResult);
                    });
                }, tmpPort);
            } catch (Exception e) {
                DLog.Error("[NetSession] " + e.ToString());
                Close();
            }
        }

        public void Register(int msgType, NetworkMessage.MessageHandlerType handler)
        {
            m_msgHandlers[msgType] = handler;
        }

        public void Register(NetworkMessage.MessageHandlerType handler)
        {
            m_defaultMsgHandler = handler;
        }

        // public bool Send(int msgType, NetworkMessage msg)
        // {
        //     if (!IsConnected())
        //         return false;
        //     
        //     // 序列化消息
        //     if (msg != null)
        //     {
        //         // 有消息体的消息
        //         SPack sp = new SPack();
        //         sp.offset = HEADER_SIZE;
        //         sp.buf = new byte[BUFFER_SIZE];
        //         
        //         sp.Write((int)msgType);
        //         msg.Encode(sp);
        //
        //         int size = (sp.offset - HEADER_SIZE); // realSize
        //         // if (size < UInt32.MaxValue)
        //         {
        //             // max 2^32 bytes length data
        //             sp.offset = 0;
        //             sp.Write(size);
        //             Array.Resize(ref sp.buf, size + HEADER_SIZE);
        //         }
        //         // else
        //         // {
        //         //     // max 2^31 bytes length data
        //         //     sp.offset = 0;
        //         //     sp.Write((UInt16) 0xFFFF); // 前2字节固定写 0xFFFF
        //         //     sp.Write(size); // 然后接实际大小
        //         // }
        //         
        //         if (m_encrypt != null) {
        //             m_encrypt.Codec(sp.buf, sp.offset, size);
        //         }
        //         Send(sp.buf);
        //     }
        //     else
        //     {
        //         // 无消息体的消息
        //         byte[] buf = new byte[NOBODY_BUFFER_SIZE];
        //         int offset = 0;
        //         NetworkSerialization.Write(ref buf, ref offset, (NOBODY_BUFFER_SIZE-HEADER_SIZE));
        //         NetworkSerialization.Write(ref buf, ref offset, (int)msgType);
        //         if (m_encrypt != null) {
        //             m_encrypt.Codec(buf, HEADER_SIZE, buf.Length - HEADER_SIZE);
        //         }
        //         Send(buf);
        //     }
        //     
        //     return true;
        // }

        public bool Send(int msgType, IMessage msg)
        {
            if (msgType != (int)ApiType.HeartBeatReqAck && msg == null)
            {
                DLog.Error("[NetSession] 发送:{0} 失败, body is nil", msgType);
                return false;
            }

            if (!IsConnected())
            {
                DLog.Error($"[NetSession] 发送:{msgType} 失败,链接已经断开");
                return false;
            }

            Client2Server s2cMsg = ProtoPool.Get<Client2Server>();
            // Client2Server s2cMsg = new Client2Server();

            int pooled = 1;

            s2cMsg.Api = (ApiType) msgType;
            s2cMsg.Data = msg.ToByteString(ref pooled);
            s2cMsg.StampMs = TimeUtils.GetCurServerTimeMSec();
            SPack sp = new SPack();
            sp.offset = HEADER_SIZE;

            pooled = 1;
            byte[] msgBytes = s2cMsg.ToByteArray(ref pooled);

            Int32 total = (pooled <= 0 ? msgBytes.Length : pooled) + HEADER_SIZE;
            sp.buf = ArrayPool<byte>.Shared.Rent(total); //  new byte[total];
            // sp.buf = new byte[total];

            if (pooled != 0)
            {
                sp.Write(msgBytes, pooled);
            }
            else
            {
                sp.Write(msgBytes);
            }

            int size = sp.offset - HEADER_SIZE;
            sp.offset = 0;
            sp.Write(size);

            var bytes = s2cMsg.Data.origin;

            DLog.Log($"[NetSession] 发送消息: Api:{msgType}, 长度:{size}");

            // var result = Send(sp.buf, total);
            Send(sp.buf, total);

            ArrayPool<byte>.Shared.Return(msgBytes);
            ArrayPool<byte>.Shared.Return(bytes);
            
            // return result;
            
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
            HandleTasks();
        }

        public void Close()
        {
            // if(!this.closeThread)
            // {
            //     this.closeThread = true;
            //     if (this.readThread != null)
            //     {
            //         this.readThread.Abort();
            //         this.readThread = null;
            //     }
            //     // this.writeThread = null;
            //     DLog.Log("标记关闭读写线程");
            // }
        
            if (m_socket == null) {
                return;
            }
            DLog.Log("Close socket!");
            
            try
            {
                m_socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                DLog.Error(e.ToString());
            }
            finally
            {
                m_socket.Close();
                m_socket = null;
                
                // //关闭读线程
                // try
                // {
                //     if (this.readThread != null)
                //     {
                //         DLog.Log("关闭线程 读线程状态:" + this.readThread.ThreadState);
                //         if ((this.readThread.ThreadState & System.Threading.ThreadState.AbortRequested) != 0 && (this.readThread.ThreadState & System.Threading.ThreadState.Aborted) != 0)
                //         {
                //             this.readThread.Abort();            //关闭线程
                //         }
                //         DLog.Log("关闭读线程成功");
                //         this.readThread = null;
                //     }
                // }
                // catch (Exception ex)
                // {
                //     DLog.Log("关闭 读socket数据线程 失败 msg:" + ex.Message);
                // }
            }
            
            Reset();
            // if (IsConnected()) {
            //     DLog.Error("[NetSession] Failed to close socket!!!!!!");
            // }

            m_disconnectionHandler?.Invoke();
        }

        public bool IsConnected()
        {
            return m_socket != null && m_socket.Connected;
        }

        #endregion
        
        byte[] Combine(byte[] b1, byte[] b2, int size)
        {
            if (b1 == null) {
                // byte[] ret = new byte[size];
                byte[] ret = ArrayPool<byte>.Shared.Rent(size);
                Array.Copy(b2, ret, size);
                return ret;
            }
            else
            {
                // byte[] combined = new byte[b1.Length + size];
                byte[] combined = ArrayPool<byte>.Shared.Rent(b1.Length + size);
                Array.Copy(b1, combined, b1.Length);
                Array.Copy(b2, 0, combined, b1.Length, size);
                return combined;
            }        
        }
        
        private void ReturnSo(StateObject so)
        {
            so.m_needbytes = 0;
            so.m_totalbytes = 0;
            so.m = null;
            so.m_buffer = null;
            so.m_mainBuffer = null;
            so.m_handler = null;

            stateObjectPool.Push(so);
        }
        
        void Reset()
        {
            ClearTaskQueue();
            //      lock (_locker) {
            //          // DLog.Log("Lock |+, Socket Reset");
            // if (m_sendQueue.Count>0)
            //  DLog.Error("ei] sendQueue size ["+m_sendQueue.Count+"], isSending "+m_isSending);
            //          m_sendQueue.Clear();
            // m_isSending = false;
            //          // DLog.Log("Lock |-, Socket Reset");
            //      }

            // m_decrypt = null;
            // m_encrypt = null;
        }

        #region SendMsg

        class SendObj : ISimpleMemoryClean
        {
            public Socket socket;
            public byte[] buf;
            public int length;
            private static SimpleMemoryPool<SendObj> objPool = new SimpleMemoryPool<SendObj>(4);

            public static SendObj Get()
            {
                return objPool.Get();
            }

            public static void Return(SendObj o)
            {
                objPool.Return(o);
            }

            public void Reset()
            {
                socket = null;
                length = 0;
                buf = null;
            }
        }

        void Send(byte[] buf, int bufSize)
        {
            if (!IsConnected()) {
                DLog.Error("[NetSession] 发送失败,链接已经断开");
                return;
            }
            
            // DLog.Log("[NetSession] +++send msg START. len:" + buf.Length);
            try
            {
                SendObj sendObj = SendObj.Get();
                sendObj.socket = m_socket;
                sendObj.buf = buf;
                sendObj.length = bufSize;

                m_socket.BeginSend(buf, 0, bufSize, 0, SendCallback, sendObj);
            }
            catch (SocketException e)
            {
                DLog.Error("[NetSession] 发送失败,SocketException:" + e.ToString());
                Close();
            }

            // lock (_locker) {
            //           // DLog.Log("Lock |++, SendStart");
            //  m_sendQueue.Add(buf);
            //  if (!m_isSending) {
            //   m_isSending = true;
            //   byte[] sbuf = m_sendQueue[0];
            //   try {
            //                   // sendInstanceTime = ext.getSysTime();
            //                   //if(NetMessageMgr.DebugLog)
            //                   //    DLog.Log("send msg at {0} {1}", sendInstanceTime, m_sendQueue.Count);
            //                   m_socket.BeginSend(sbuf, 0, sbuf.Length, 0, SendCallback, m_socket);
            //   } catch (SocketException e) {
            //    DLog.Log(e.ToString());
            //    Close();
            //   }
            //  }
            //           // DLog.Log("Lock |--, SendStart");
            // }
        }

        void SendCallback(IAsyncResult ar)
        {
            QueueOnMainThread(() => SendCallbackMainThread(ar));
        }
        
        void SendCallbackMainThread(IAsyncResult ar)
        {
            SendObj sendObj = (SendObj)ar.AsyncState;
            Socket socket = sendObj.socket;
            if (!socket.Connected) {
                return;
            }

            int send = 0;
            try
            {
                send = socket.EndSend(ar);
            }
            catch (SocketException e)
            {
                DLog.Error("[NetSession] 发送失败,SocketException:" + e.ToString());
                Close();
                return;
            }

            if (send == sendObj.length)
            {
                DLog.Log($"[NetSession] 发送完成,长度:{send}");
                
                // 发送完成，回收buf
                ArrayPool<byte>.Shared.Return(sendObj.buf, true);
            }
            else if (send < sendObj.length)
            {
                DLog.Log($"[NetSession] 部分发送完成,长度:{send}/{sendObj.length}");
                
                // 一次没发完，重新补发剩下的
                byte[] newBuf = ArrayPool<byte>.Shared.Rent(sendObj.length - send);
                Array.Copy(sendObj.buf, send, newBuf, 0, sendObj.length - send);
                
                ArrayPool<byte>.Shared.Return(sendObj.buf, true);
                Send(newBuf, sendObj.length - send);
            }
            
            SendObj.Return(sendObj);
            
            
            // try {
            //     lock (_locker)
            //     {
            //         // DLog.Log("Lock |+++, SendCallback");
            //         int send = sock.EndSend(ar);
            //         if (send == m_sendQueue[0].Length) {
            //             // DLog.Log($"[NetSession] 发送完成,长度:{send}");
            //             m_sendQueue.RemoveAt(0);
            //         } else if (send < m_sendQueue[0].Length) {
            //             int newBufSize = m_sendQueue[0].Length - send;
            //             byte[] newBuf = new byte[newBufSize];
            //             Array.Copy(m_sendQueue[0], send, newBuf, 0, newBufSize);
            //             m_sendQueue[0] = newBuf;
            //         }
            //
            //         if (m_sendQueue.Count > 0) {
            //             byte[] buf = m_sendQueue[0];
            //             m_socket.BeginSend(buf, 0, buf.Length, 0, SendCallback, m_socket);
            //         } else {
            //             m_isSending = false;
            //         }
            //         // DLog.Log("Lock |---, SendCallback");
            //     }
            // } catch (SocketException e) {
            //     DLog.Error("[NetSession] Send callback error: " + e.ToString());
            //     Close();
            // }
        }
        

        #endregion
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
        
        // 尝试连接目标域名时，进行 DNS解析回调， 解析完成后，会尝试连接目标IP:PORT地址
        void GetHostEntryCallbackMainThread(IAsyncResult asyncResult)
        {
            int? port = asyncResult.AsyncState as int?;
            IPAddress[] ipAddr = Dns.EndGetHostAddresses(asyncResult);
            if (ipAddr != null)
            {
                List<IPAddress> addrList = new List<IPAddress>(ipAddr);
                if (port != null)
                {
                    DoConnect(addrList, port.Value);
                }
                else
                {
                    DLog.Error("[NetSession] DNS error: can't get port.");
                    Close();
                }
            }
            else
            {
                DLog.Error("[NetSession] DNS error: can't parse address.");
            }
        }
        
        bool ConnectCallBackMainThread(IAsyncResult asyncResult)
        {
            Socket sock = (Socket)asyncResult.AsyncState;
            try {
                sock.EndConnect(asyncResult);
            } catch (Exception e) {
                DLog.Error("ConnectCallBackMainThread" + e.ToString());
                return false;
            }
            if (m_connectSuccessHandler != null) {
                m_connectSuccessHandler();
            }

            // DoRead();
            //创建读写线程
            // CreateThreads();

            DoReadData();
            return true;
        }

        // void ReadBodyCallback(byte[] buf)
        // {
        //     if (buf == null) {
        //         QueueOnMainThread(() => {
        //             NetMessageMgr.OnConnectFailed(NetMessageMgr.ConnectCode.ReadBodyDataError, "read body error");
        //         });
        //         return;
        //     }
        //     if (m_decrypt != null) {
        //         m_decrypt.Codec(buf, 0, buf.Length);
        //     }
        //
        //     QueueOnMainThread(() => {
        //         HandleRawdata(buf);
        //     });
        //
        // }


        // 这里已经属于业务层的数据了，可以交给业务层去处理
        void HandleRawdata(byte[] buf, int bufSize)
        {
            var span = new ReadOnlySequence<byte>(buf, 0, bufSize);
            Server2Client s2cMsg = Server2Client.Parser.ParseFrom(span);
            ArrayPool<byte>.Shared.Return(buf, true);
            DLog.Log($"[NetSession] 解包: msgType:{s2cMsg.Api}, len:{buf.Length}");

            // var msgType = s2cMsg.Api;
            HandleMsg(s2cMsg);

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

        // private int InitPike(byte[] buf, int offset)
        // {
        //     UInt32 key1 = NetworkSerialization.ReadUInt32(buf, ref offset);
        //     UInt32 key2 = NetworkSerialization.ReadUInt32(buf, ref offset);
        //
        //     if (key1 == 0 && key2 == 0) {
        //         return offset;
        //     }
        //
        //     var token = ((Int64)(key1) << 32) | (Int64)(key2);
        //     var u1 = (UInt32)(token >> 3) & 0xFF;
        //     var u2 = (UInt32)(token >> 23) & 0xFF;
        //     var u3 = (UInt32)(token >> 37) & 0xFF;
        //     var u4 = (UInt32)(token >> 47) & 0xFF;
        //     var key = (u1 << 24) | (u3 << 16) | (u2 << 8) | u4;
        //
        //     m_decrypt = new Pike(key);
        //     m_encrypt = new Pike(key);
        //     // m_piketest = new Pike(key);
        //     return offset;
        // }

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

        // 这里由业务层来实现, Server2Client 是S2C结构的基础类
        private void HandleMsg(Server2Client s2cMsg)
        {
            NetworkMessage.MessageHandlerType handler = null;

            if (!m_msgHandlers.TryGetValue((int)s2cMsg.Api, out handler))
            {
                //没有注册的句柄就放进去,默认会添加到消息队列中
                //NetManager.InitSession中的Register
                handler = m_defaultMsgHandler;
            }
            
            if (handler == null)
            {
                DLog.Error("Cannot handle {0}!", s2cMsg.Api);
                return;
            }
            
            NetworkMessage netMsg = NetworkMessage.MakeNewMsg(
                (int)s2cMsg.Api,
                new ReadOnlySequence<byte>(s2cMsg.Data.Memory),
                s2cMsg.StampMs,
                s2cMsg.MainStatus, s2cMsg.SubStatus,
                s2cMsg.ConnectTime, s2cMsg.Client2ServerStampMs, s2cMsg.SequenceId
            );

            handler(netMsg);
        }

        // private int HandleMsg(byte[] buf, int offset, int msgType)
        // {
        //     NetworkMessage.MessageHandlerType handler = null;
        //     if (!m_msgHandlers.TryGetValue(msgType, out handler)) {
        //         handler = m_defaultMsgHandler;
        //     }
        //
        //     if (handler == null) {
        //         if (NetMessageMgr.DebugLog)
        //             DLog.Log("Cannot handle {0}!", msgType);
        //         return offset;
        //     }
        //     //if(!NetMessageMgr.BlockMsgDebug)
        //     //    NetMessageMgr.ResetHeartbeatTimer();
        //     // NetworkMessage msg = Protocol.CreateMessage(msgType);
        //     // if (msg == null) {
        //     //     msg = new NetworkMessage();
        //     //     msg.SetMessageType(msgType);
        //     //     msg.m_nodId = nodeId;
        //     //     handler(msg);
        //     // } else {
        //     //     SPack sp = new SPack();
        //     //     sp.buf = buf;
        //     //     sp.offset = offset;
        //     //     msg.Decode(sp);
        //     //     msg.m_nodId = nodeId;
        //     //     handler(msg);
        //     // }
        //
        //     return offset;
        // }

        void DoConnect(List<IPAddress> addrList, int port)
        {
            if (addrList.Count == 0) {
                DLog.Error("[NetSession] Failed to connect the gate entry!");
                Close();
                return;
            }
            IPAddress addr = addrList[0];
            DLog.Log("[NetSession] Try to connect entry {0} {1}", addr, addr.AddressFamily);
            // lock (lockObj) //socket保护
            {
                // DLog.Log("Lock +, DoConnect");
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
                
                DLog.Log($"[NetSession] 接受缓冲区大小:{m_socket.ReceiveBufferSize}");

                // isBeginConnect = true;
                // IAsyncResult asyncResult = m_socket.BeginConnect(ipe, null, m_socket);
                // while (true)
                // {
                //     if (asyncResult.IsCompleted)
                //     {
                //         isBeginConnect = false;
                //         if (!ConnectCallBackMainThread(asyncResult))
                //         {
                //             
                //         }
                //     }
                // }
                    
                m_socket.BeginConnect(ipe, asyncResult =>
                {
                    QueueOnMainThread(() =>
                    {
                        // 尝试连接，如果连接异常，就尝试下一个地址; 如果连接成功，就准备接收数据
                        if (!ConnectCallBackMainThread(asyncResult))
                        {
                            addrList.RemoveAt(0);
                            DoConnect(addrList, port); //ei:if addrList null, need tips check network or retry by hand
                        }
                    });
                }, m_socket);
                
                // DLog.Log("Lock -, DoConnect");
            }
        }
        
        // private void CreateThreads()
        // {
        //     lock(lockObj)
        //     {
        //         // DLog.Log("Lock ++, CreateReadThread");
        //         if (this.IsConnected())
        //         {
        //             this.closeThread = false;
        //             this.readThread = new System.Threading.Thread(this.ReadData);   //接收使用独立线程
        //             this.readThread.IsBackground = true;
        //             this.readThread.Start(m_socket);
        //
        //             // this.writeThread = new Thread(this.writeData);  //写数据线程
        //             // this.writeThread.IsBackground = true;
        //             // this.writeThread.Start(this.m_socket);
        //             
        //             DLog.Warning("启动读写数据线程  线程ID = " + readThread.ManagedThreadId.ToString());
        //         }   
        //         
        //         // DLog.Log("Lock --, CreateReadThread");
        //     }
        // }

        // private void DoReadData()
        // {
        //     ReadData(m_socket);
        // }
        
        private void DoReadData()
        {
            // 先读包头
            Read(HEADER_SIZE, ReadHeaderData, true);

            // 以下为采用创建独立线程的方式读取数据, 需要注意线程的创建和回收
            // DLog.Log("创建读取数据  线程ID = " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
            //
            // // Socket client = o as Socket;
            // var stateObj = new StateObject();
            // //先读头获取协议体大小
            // stateObj.m_socket = m_socket;
            // stateObj.m_needbytes = HEADER_SIZE;
            // stateObj.m_totalbytes = HEADER_SIZE;
            // int read = 0;
            //
            // while (true)
            // {
            //     // stateObj.m_buffer = new byte[stateObj.m_needbytes];
            //     stateObj.m_buffer = ArrayPool<byte>.Shared.Rent(stateObj.m_needbytes);
            //
            //     try
            //     {
            //         m_socket.BeginReceive(stateObj.m_buffer, 0, stateObj.m_needbytes, 0, ReadCallback, stateObj);
            //         // read = m_so.Receive(stateObj.m_buffer, 0, stateObj.m_needbytes, 0);
            //         
            //         // DLog.Log($"[NetSession] EndReceive Length:{stateObj.m_needbytes}/{stateObj.m_totalbytes} Header:{stateObj.header}, Length:{read}");
            //         
            //         // lock (lockObj)
            //         {
            //             // DLog.Log("Lock +++, ReadData Header");
            //             // if (closeThread)
            //             // {
            //             //     DLog.Warning("readData 退出线程");
            //             //     break;
            //             // }
            //
            //             // if (client != m_socket)
            //             // {
            //             //     DLog.Warning("readData socket 已经更换 退出线程 : " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
            //             //     break;
            //             // }
            //             
            //             // 读取包头数据
            //             // if (read > 0)
            //             // {
            //             //     // 接收到一个包，开始处理（先读取包大小，然后读取包体）
            //             //     // int length = stateObj.m_totalbytes - stateObj.m_needbytes;
            //             //     
            //             //     stateObj.m_mainBuffer = Combine(stateObj.m_mainBuffer, stateObj.m_buffer, read);
            //             //     stateObj.m_needbytes -= read;
            //             //     if (stateObj.m_needbytes == 0)
            //             //     {
            //             //         int offset = 0;
            //             //         int size = NetworkSerialization.ReadInt32(stateObj.m_mainBuffer, ref offset);
            //             //         // DLog.Log($"[NetSession] ReadHeaderCallback, Size:{size}");
            //             //         // int size = stateObj.isBigPack
            //             //         //     ? NetworkSerialization.ReadInt32(stateObj.m_mainBuffer, ref offset)
            //             //         //     : NetworkSerialization.ReadUInt16(stateObj.m_mainBuffer, ref offset);
            //             //         // DLog.Log("[NetSession] READ DATA, recv msg size:" + size);
            //             //         // if(size == 0xFFFF && !stateObj.isBigPack)
            //             //         // {
            //             //         //     DLog.Warning("recv a big packet, use 4 bytes header !!");
            //             //         //     // Read(4, Read4ByteHeaderCallback);
            //             //         //     stateObj.m_needbytes = 4;
            //             //         //     stateObj.m_mainBuffer = null;
            //             //         //     stateObj.isBigPack = true;
            //             //         // }
            //             //         // else
            //             //         {
            //             //             var stateBodyObj = new StateObject
            //             //             {
            //             //                 m_socket = client,
            //             //                 m_needbytes = size,
            //             //                 m_totalbytes = size
            //             //             };
            //             //
            //             //             if (!ReadBodyData(stateBodyObj)) break;
            //             //     
            //             //             stateObj.m_needbytes = HEADER_SIZE;
            //             //             stateObj.m_mainBuffer = null;
            //             //             // stateObj.isBigPack = false;
            //             //         }
            //             //     }
            //             // }
            //             // else
            //             // {
            //             //     // 与服务器断开连接，可能服务器关闭，或者被服务器踢掉了
            //             //     if (NetMessageMgr.DebugLog)
            //             //         DLog.Error("ReadCallback, Length: " + read + " IsConnected:" + IsConnected());
            //             //     QueueOnMainThread(() => {
            //             //         NetMessageMgr.OnConnectFailed(NetMessageMgr.ConnectCode.ReadDataError, "read error");
            //             //     });
            //             //     break;
            //             // }
            //             // DLog.Log("Lock ---, ReadData Header");
            //         }
            //     }
            //     catch (SocketException e)
            //     {
            //         DLog.Error("[NetSession] ReadCallback, SocketException: " + e.ToString());
            //         break;
            //     }
            // }
        }

        // 共享包数据的对象池
        private Stack<StateObject> stateObjectPool = new Stack<StateObject>(4);
        // 读取包头和包体流程是一样的，就统一到一起
        private bool Read(int size, RawdataHandler handler, bool isHeader)
        {
            if (size <= 0)
            {
                DLog.Error("Read, size <= 0");
                return false;
            }
            
            if (!IsConnected())
                return false;

            StateObject stateObj = null;
            if (stateObjectPool.Count > 0)
            {
                stateObj = stateObjectPool.Pop();
            }

            if (stateObj == null)
            {
                DLog.Warning("[NetSession] stateObject pop failed, new one");
                stateObj = new StateObject();
            }

            stateObj.m_socket = m_socket;
            stateObj.m_needbytes = size;
            stateObj.m_totalbytes = size;
            stateObj.m_handler = handler;
            stateObj.header = isHeader;
            stateObj.m_mainBuffer = null;
            stateObj.m_buffer = null;

            // 开始尝试读数据
            DoRead(stateObj);
            return true;
        }

        // 单纯读取数据
        private void DoRead(StateObject stateObj)
        {
            stateObj.m_buffer = ArrayPool<byte>.Shared.Rent(stateObj.m_needbytes);

            try
            {
                m_socket.BeginReceive(stateObj.m_buffer, 0, stateObj.m_needbytes, 0, ReadCallback, stateObj);
                // read = m_so.Receive(stateObj.m_buffer, 0, stateObj.m_needbytes, 0);
                // DLog.Log($"[NetSession] EndReceive Length:{stateObj.m_needbytes}/{stateObj.m_totalbytes} Header:{stateObj.header}, Length:{read}");
            }
            catch (SocketException e)
            {
                DLog.Error("[NetSession] DoRead, SocketException: " + e.ToString());
                QueueOnMainThread(() =>
                {
                    NetMessageMgr.OnConnectFailed(stateObj.header ? NetMessageMgr.ConnectCode.ReadHeadDataError : NetMessageMgr.ConnectCode.ReadBodyDataError, $"read error: {stateObj.header}");
                });
            }
        }
        
        #region 异步Read

        private void ReadCallback(IAsyncResult ar)
        {
            StateObject stateObj = (StateObject) ar.AsyncState;
            if (!stateObj.m_socket.Connected)
            {
                // DLog.Error("ReadCallback, socket is not connected!");
                return;
            }

            int read = 0;
            try
            {
                read = stateObj.m_socket.EndReceive(ar);
            }
            catch (SocketException e)
            {
                DLog.Error(e.ToString());
                return;
            }

            if (read > 0)
            {
                // 读取原始数据, m_needbytes可能大于read，表示一个大包被拆开了， TODO： 这种情况待测试
                stateObj.m_mainBuffer = Combine(stateObj.m_mainBuffer, stateObj.m_buffer, read);
                stateObj.m_needbytes -= read;
                
                // 将buffer返回到池中
                ArrayPool<byte>.Shared.Return(stateObj.m_buffer, true);
                stateObj.m_buffer = null;
                
                if (stateObj.m_needbytes == 0)
                {
                    // 即将解析数据， 可能是包头或包体
                    stateObj.m_handler(stateObj.m_mainBuffer, stateObj.m_totalbytes);
                    ReturnSo(stateObj);
                }
                else
                {
                    // 正常不应该还有数据
                    DLog.Error($"[NetSession] ReadCallback, m_needbytes: {stateObj.m_needbytes}");
                }
            }
            else
            {
                DLog.Error("[NetSession] ReadCallback: " + read + " IsConnected:" + IsConnected());
                QueueOnMainThread(() =>
                {
                    NetMessageMgr.OnConnectFailed(NetMessageMgr.ConnectCode.ReadZeroData, $"read 0 byte error");
                });
            }
        }

        
        private void ReadHeaderData(byte[] buf, int bufSize)
        {
            if (buf == null)
            {
                DLog.Error("[NetSession] ReadHeaderData, buf is null");
                QueueOnMainThread(() => {
                    NetMessageMgr.OnConnectFailed(NetMessageMgr.ConnectCode.ReadHeadDataError, "read header error");
                });
                return;
            }
            
            int offset = 0;
            int size = NetworkSerialization.ReadInt32(buf, ref offset);

            if (size <= 0)
            {
                DLog.Error("[NetSession] ReadHeaderData, size <= 0");
                // 包体长度为0 ？ 可能数据出错了，可能需要断开连接
                // QueueOnMainThread(() => {
                //     NetMessageMgr.OnConnectFailed(NetMessageMgr.ConnectCode.ReadHeadDataError, "read error");
                // });
                return;
            }

            // 包头读完了读包体
            Read(size, ReadBodyData, false);
        }
        
        private void ReadBodyData(byte[] buf, int bufSize)
        {
            if (buf == null)
            {
                DLog.Error("[NetSession] ReadBodyData, buf is null");
                QueueOnMainThread(() => {
                    NetMessageMgr.OnConnectFailed(NetMessageMgr.ConnectCode.ReadBodyDataError, "read body error");
                });
                return;
            }
            
            // 包体读完了,再继续读下个包头, 完成循环接收
            DoReadData();

            QueueOnMainThread(() => {
                HandleRawdata(buf, bufSize);
            });
        }

        #endregion
    }
}