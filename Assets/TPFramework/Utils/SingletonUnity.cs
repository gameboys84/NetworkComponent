using UnityEngine;

namespace TPFramework
{
    public class SingletonUnity<T> : MonoBehaviour where T : SingletonUnity<T>
    {
        /// <summary>
        ///   单例实例
        /// </summary>
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.FindObjectOfType(typeof(T)) as T;
                    if (_instance == null)
                        _instance = new GameObject("SingletonUnity_" + typeof(T), typeof(T)).GetComponent<T>();

                    DontDestroyOnLoad(_instance);
                }
                return _instance;
            }
        }

        /// <summary>
        ///  创建单例实例
        /// </summary>
        public static T CreateSingleton()
        {
            return Instance;
        }

        /// <summary>
        ///   确保在程序退出时销毁实例。
        /// </summary>
        private void OnApplicationQuit()
        {
            _instance = null;
        }
    }
}