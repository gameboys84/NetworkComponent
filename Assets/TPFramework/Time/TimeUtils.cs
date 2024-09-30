using System;

namespace TPFramework
{
    public class TimeUtils
    {
        private static long _localTime = 0;
        private static long _serverTime = 0;

        public static long GetSystemTime()
        {
            return Environment.TickCount; //System.DateTime.Now.Ticks;
        }

        public static void SyncServerTime(long serverTime)
        {
            _localTime = GetSystemTime(); //System.DateTime.Now.Ticks;
            _serverTime = serverTime;
        }
        
        /// <summary>
        /// Get the current server time in milliseconds
        /// </summary>
        /// <returns></returns>
        public static long GetCurServerTimeMSec()
        {
            return GetSystemTime() - _localTime + _serverTime;
        }

        /// <summary>
        /// Get the current server time in seconds
        /// </summary>
        /// <returns></returns>
        public static long GetCurServerTimeSec()
        {
            return GetCurServerTimeMSec() / 1000;
        }
    }
}