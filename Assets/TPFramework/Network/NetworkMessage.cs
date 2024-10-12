using System;
using System.Buffers;

namespace TPFramework
{
    public class NetworkMessage
    {
        public delegate void MessageHandlerType(NetworkMessage msg);
        // Protocol.MsgType m_type;

        public ReadOnlySequence<byte> Data;
    
        public Int64 stamp;
    
        private int msgType;

        public Int64 SequenceId;
        public Int64 connectTime;
        public Int64 client2ServerStampMs;
    
        public string mainStatus;
        public string subStatus;

        public int GetMsgType()
        {
            return msgType;
        }

        // ENCODE AND DECODE METHODS HERE
        public virtual void Encode(SPack sp)
        {
            DLog.Error("NetworkMessage.Encode() not implemented!");
        }

        public virtual void Decode(SPack sp)
        {
            DLog.Error("NetworkMessage.Decode() not implemented!");
        }
        
        public static NetworkMessage MakeNewMsg(int api, ReadOnlySequence<byte> data, Int64 stamp, string main, string sub, 
            Int64 connectTime, Int64 client2ServerStampMs, Int64 sequenceId)
        {
            NetworkMessage msg = new NetworkMessage
            {
                msgType = api,
                Data = data,
                stamp = stamp,
                mainStatus = main,
                subStatus = sub,
                connectTime = connectTime,
                client2ServerStampMs = client2ServerStampMs,
                SequenceId = sequenceId
            };
        
            return msg;
        }
    }
}