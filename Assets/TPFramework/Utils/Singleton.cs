using System;

namespace TPFramework
{
    public class Singleton<T> where T : new()
    {
        // 单件实例对象
        private static T _instance;

        /// <summary>
        /// 获取单件对象
        /// </summary>
        /// <value>单件实例</value>
        public static T Instance
        {
            get
            {
                // 没有单件，则立即创建一个
                // Thread Unsafe
                if (_instance == null)
                    _instance = ((default(T) == null) ? Activator.CreateInstance<T>() : default);

                return _instance;
            }
        }

        /// <summary>
        /// 清理单件对象
        /// </summary>
        public void CleanInstance()
        {
            _instance = default;
        }
    }
}