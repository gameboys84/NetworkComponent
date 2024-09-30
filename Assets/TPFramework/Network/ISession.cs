using System;

namespace TPFramework
{
    public interface ISession
    {
        void Connect(string host, int port);
        void Register(Protocol.MsgType msgType, NetworkMessage.MessageHandlerType handler);
        void Register(NetworkMessage.MessageHandlerType handler);
        bool Send(Protocol.MsgType msgType, NetworkMessage msg);
        void RegisterDisconnectionHandler(Action handler);
        void RegisterConnectSuccessHandler(Action handler);
        void UpdateSession();
        void Close(bool force = false);
        bool IsConnected();
    }
}