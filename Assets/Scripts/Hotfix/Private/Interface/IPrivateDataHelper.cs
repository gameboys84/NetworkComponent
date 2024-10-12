using Google.Protobuf;

public interface IPrivateDataHelper
{
    string GetServerIP();
    int GetServerPort();
    T MakePrivateData<T>() where T : class, IMessage<T>;
}
