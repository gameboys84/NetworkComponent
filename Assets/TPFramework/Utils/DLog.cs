using System.Diagnostics;

namespace TPFramework
{
    public static class DLog
    {
        [Conditional("ENABLE_LOG")]
        public static void Log(string msg, params object[] para)
        {
            UnityEngine.Debug.LogFormat(msg, para);
        }
        [Conditional("ENABLE_LOG")]
        public static void Log(string msg)
        {
            UnityEngine.Debug.Log(msg);
        }

        [Conditional("ENABLE_LOG")]
        public static void Warning(string msg, params object[] para)
        {
            UnityEngine.Debug.LogWarningFormat(msg, para);
        }
        [Conditional("ENABLE_LOG")]
        public static void Warning(string msg)
        {
            UnityEngine.Debug.LogWarning(msg);
        }

        [Conditional("ENABLE_LOG")]
        public static void Error(string msg, params object[] para)
        {
            UnityEngine.Debug.LogErrorFormat(msg, para);
        }
        [Conditional("ENABLE_LOG")]
        public static void Error(string msg)
        {
            UnityEngine.Debug.LogError(msg);
        }
    }
}