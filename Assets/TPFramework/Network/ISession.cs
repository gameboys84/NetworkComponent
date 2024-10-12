using System;
using Google.Protobuf;

namespace TPFramework
{
    public interface ISession
    {
        void Connect(string host, int port);
        void Register(int msgType, NetworkMessage.MessageHandlerType handler);
        void Register(NetworkMessage.MessageHandlerType handler);
        // bool Send(int msgType, NetworkMessage msg);
        bool Send(int msgType, IMessage msg);
        void RegisterDisconnectionHandler(Action handler);
        void RegisterConnectSuccessHandler(Action handler);
        void UpdateSession();
        void Close();
        bool IsConnected();
    }
}