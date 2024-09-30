namespace TPFramework
{
    public class Protocol
    {
        public static int Ver = 1;
        public static int RunVer = 1;
        
        public enum MsgType
        {
            LOGIN_REQ = 1,
            LOGIN_ACK = 2,
        }
    }
}