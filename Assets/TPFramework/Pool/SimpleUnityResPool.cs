using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPFramework
{
    public class UnityComponentPoolHandler : ISimpleMemoryClean
    {
        public Component component;
        public string resKey;
        public GameObject origin;
        public float gotTime;
        private float _autoReturnTime;
        public bool isUsing = false;
        
        private UnityComponentAutoReturn autoReturnComponent;
        
        public float autoReturnTime
        {
            get => _autoReturnTime;
            set
            {
                _autoReturnTime = value;
                if (value > 0)
                {
                    autoReturnComponent = component.gameObject
                        .GetOrAddComponent<UnityComponentAutoReturn>();
                    autoReturnComponent.enabled = true;
                    autoReturnComponent.Set(this);
                }
            }
        }
        
        public void Return()
        {
            if (!isUsing)
            {
                DLog.Error("return is not using");
                return;
            }
            
#if UNITY_EDITOR
            if (component == null || component.gameObject == null)
            {
                DLog.Error($"return obj is nil:{resKey}");
                //return;
            }
#endif

            isUsing = false;
            component.transform.SetParent(UnityComponentPoolManager.Instance.transform);
            if (string.IsNullOrEmpty(resKey))
            {
                DLog.Error("ResKey is null.");
            }
            else
            {
                UnityComponentPoolManager.Instance
                    .GetExistPool(resKey)
                    .ReturnHandler(this);
            }
        }

        public void Reset()
        {
            component = null;
            resKey = null;
            gotTime = 0;
            autoReturnTime = -1;
        }
    }
    
    
    public class UnityComponentPool: ObjectPool<Component>
    {
        private static SimpleMemoryPool<UnityComponentPoolHandler> handlerPool;
        private List<UnityComponentPoolHandler> used;
        private Component asset;

        public Transform root;

        private string resKey;
        private GameObject orginGo;

        public void SetRoot(Transform parent = null)
        {
            if (parent == null)
            {
                GameObject go = new GameObject();
                root = go.transform;
                root.SetParent(UnityComponentPoolManager.Instance.transform);
                root.localPosition = Vector3.zero;
#if UNITY_EDITOR
                root.name = $"{System.IO.Path.GetFileNameWithoutExtension(resKey)}_Pool";
#endif
            }
            else
            {
                root = parent;
            }
        }
        
//         public async UniTask Load<T>(string rk) where T : Component
//         {
//             resKey = rk;
//             var go = await AddressableManager.Instance.LoadAsset<GameObject>(resKey);
//             
//             if (go == null)
//             {
//                 DLog.Error($"Pool指定的Asset为空:Path:{resKey}");
//                 return;
//             }
//             asset = go.GetComponent<T>();
//
//             if (asset == null)
//             {
//                 DLog.Error($"Pool指定的Asset无法GetComponent成功:{nameof(T)}");
//             }
//
// #if UNITY_EDITOR
//             pn = System.IO.Path.GetFileNameWithoutExtension(this.resKey);
// #endif
//         }
//         
//         public async void Load(string rk,Type type)
//         {
//             resKey = rk;
//             var go = await AddressableManager.Instance.LoadAsset<GameObject>(resKey);
//             if (go == null)
//             {
//                 DLog.Error($"Pool指定的Asset为空:Path:{resKey}");
//             }
//             asset = go.GetComponent(type);
//             if (asset == null)
//             {
//                 DLog.Error($"Pool指定的Asset为空:Path:{resKey}");
//             }
//             
// #if UNITY_EDITOR
//             pn = System.IO.Path.GetFileNameWithoutExtension(this.resKey);
// #endif
//         }

        public void Load<T>(GameObject originGo, string key) where T : Component
        {
            var go = originGo;
            resKey = key;
            
            if (go == null)
            {
                DLog.Error($"Pool指定的Asset为空:Path:{resKey}");
            }
            asset = go.GetComponent<T>();

            if (asset == null)
            {
                DLog.Error($"Pool指定的Asset为空:Path:{resKey}");
            }
            
#if UNITY_EDITOR
            pn = System.IO.Path.GetFileNameWithoutExtension(resKey);
#endif
        }

        public void Load(GameObject originGo, Type type, string key)
        {
            var go = originGo;
            resKey = key;
            
            if (go == null)
            {
                DLog.Error($"Pool指定的Asset为空:Path:{resKey}");
            }

            asset = go.GetComponent(type);

            if (asset == null)
            {
                DLog.Error($"Pool指定的Asset为空:Path:{resKey}");
            }
            
#if UNITY_EDITOR
            pn = System.IO.Path.GetFileNameWithoutExtension(resKey);
#endif
        }

        protected override Component CreateInstance(Transform transform = null)
        {
            if (ReferenceEquals(transform, null))
            {
                return UnityEngine.Object.Instantiate(asset, root);
            }
            else
            {
                return UnityEngine.Object.Instantiate(asset, transform);
            }
        }

        /// <summary>
        /// 获得一个pool item 对象 包含了对象本身和其他数据
        /// </summary>
        /// <param name="autoReturnTime">支持自动回收</param>
        /// <returns></returns>
        public UnityComponentPoolHandler GetWithHandler(float autoReturnTime = -1)
        {
            var component = Rent();
            handlerPool ??= new SimpleMemoryPool<UnityComponentPoolHandler>(60);
            UnityComponentPoolHandler handler  = handlerPool.Get();
            handler.component = component;
            handler.resKey = resKey;
            handler.origin = orginGo;
            handler.gotTime = Time.timeSinceLevelLoad;
            handler.autoReturnTime = autoReturnTime;
            handler.isUsing = true;
            
            return handler;
        }
        
        public Component GetOnlyObj(Transform r = null)
        {
            var ret = Rent(r);
            return ret;
        }
        
        public T GetOnlyObj<T>(Transform r = null) where T : Component
        {
            var ret = Rent(r);
            return ret as T;
        }
        
        public UnityComponentPoolHandler GetHandlerAndHold(float autoReturnTime = -1)
        {
            var handler = GetWithHandler();
            used ??= new List<UnityComponentPoolHandler>(10);
            used.Add(handler);
            return handler;
        }

        /// <summary>
        /// 通过句柄退回
        /// </summary>
        /// <param name="handler"></param>
        public void ReturnHandler(UnityComponentPoolHandler handler)
        {
            handler.isUsing = false;
            Return(handler.component);
            
            used?.Remove(handler);
            handlerPool.Return(handler);
        }
        
        
        /// <summary>
        /// 自动返回所有的,只能返回使用了GetHandlerAndHold的获得的对象
        /// </summary>
        public void ReturnAll()
        {
            if (used == null)
            {
                return;
            }
            
            for (int i = used.Count - 1; i >= 0; --i)
            {
                used[i].Return();
            }
            
            used.Clear();
        }

        protected override void OnBeforeRent(Component instance)
        {
            //base.OnBeforeRent(instance);
        }
    }

    public class UnityComponentPoolManager : SingletonUnity<UnityComponentPoolManager> //, IRestartComponent
    {
        private Dictionary<string, UnityComponentPool> poolDic;

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        
        // public async UniTask<UnityComponentPool> GetPool<T>(string resKey, Transform parent = null) where T : Component
        // {
        //     poolDic ??= new Dictionary<string, UnityComponentPool>(20);
        //
        //     if (!poolDic.TryGetValue(resKey, out var pool))
        //     {
        //         pool = new UnityComponentPool();
        //         await pool.Load<T>(resKey);
        //         pool.SetRoot(parent);
        //         poolDic.Add(resKey, pool);
        //     }
        //
        //     return pool;
        // }

        // public UnityComponentPool GetPool(string resKey, Type type, Transform parent = null)
        // {
        //     poolDic ??= new Dictionary<string, UnityComponentPool>(20);
        //
        //     if (!poolDic.TryGetValue(resKey, out var pool))
        //     {
        //         pool = new UnityComponentPool();
        //         pool.Load(resKey, type);
        //         pool.SetRoot(parent);
        //         poolDic.Add(resKey, pool);
        //     }
        //
        //     return pool;
        // }

        public UnityComponentPool GetPool<T>(GameObject origin, Transform parent = null) where T : Component
        {
            poolDic ??= new Dictionary<string, UnityComponentPool>(20);

            var resKey = origin.GetInstanceID().ToString();
            
            if (!poolDic.TryGetValue(resKey, out var pool))
            {
                pool = new UnityComponentPool();
                pool.Load<T>(origin, resKey);
                pool.SetRoot(parent);

                poolDic.Add(resKey, pool);
            }

            return pool;
        }

        public UnityComponentPool GetPool(GameObject origin, Type type, Transform parent = null)
        {
            poolDic ??= new Dictionary<string, UnityComponentPool>(20);

            var resKey = origin.GetInstanceID().ToString();

            if (!poolDic.TryGetValue(resKey, out var pool))
            {
                pool = new UnityComponentPool();

                pool.Load(origin, type, resKey);
                pool.SetRoot(parent);
                
                poolDic.Add(resKey, pool);
            }

            return pool;
        }

        public UnityComponentPool GetExistPool(string resKey)
        {
            poolDic ??= new Dictionary<string, UnityComponentPool>(20);

            if (poolDic.TryGetValue(resKey, out var pool))
            {
                return pool;
            }
            else
            {
                DLog.Warning("尝试寻找不存在的Pool");
                return null;
            }
        }
        
        public UnityComponentPool GetExistPool(GameObject origin)
        {
            poolDic ??= new Dictionary<string, UnityComponentPool>(20);

            if (poolDic.TryGetValue(origin.GetInstanceID().ToString(), out var pool))
            {
                return pool;
            }
            else
            {
                DLog.Error("尝试寻找不存在的Pool");
                return null;
            }
        }

        public void ReturnAllPoolItem(string resKey)
        {
            if (poolDic == null)
            {
                return;
            }

            if (poolDic.TryGetValue(resKey, out var pool))
            {
                pool.ReturnAll();
            }
        }

        public void RemovePool(string path)
        {
            if (poolDic == null)
            {
                return;
            }

            if (poolDic.TryGetValue(path, out var pool))
            {
                pool.Clear();
                poolDic.Remove(path);
            }
        }
        
        public void RemovePool(GameObject orin)
        {
            if (poolDic == null)
            {
                return;
            }

            var resKey = orin.GetInstanceID().ToString();
            if (poolDic.TryGetValue(resKey, out var pool))
            {
                pool.Clear();
                poolDic.Remove(resKey);
            }
        }
    }
}