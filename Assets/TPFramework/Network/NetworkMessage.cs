namespace TPFramework
{
    public class NetworkMessage
    {
        public delegate void MessageHandlerType(NetworkMessage msg);
        Protocol.MsgType m_type;
        
        public void SetMessageType(Protocol.MsgType type)
        {
            m_type = type;
        }
        
        public Protocol.MsgType GetMessageType()
        {
            return m_type;
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
    }
}