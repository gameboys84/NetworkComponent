using System;
using System.Buffers;
using System.Collections.Generic;
using Google.Protobuf;

namespace Protocol
{
    public static class ProtoPool
    {
        private static readonly Dictionary<string, List<object>> Pool = new Dictionary<string, List<object>>();

        public static readonly Dictionary<string, int> poolTypes = new Dictionary<string, int>();

#if UNITY_EDITOR
        private static readonly Dictionary<string, int> msgCounter = new Dictionary<string, int>();
#endif

        static void Add<T>(string n, int c) where T : IMessage
        {
            if (Pool.ContainsKey(n))
            {
                return;
            }
            
            var list = new List<object>(c);
            Pool.Add(n, list);
            
            for (int i = 0; i < c; ++i)
            {
                var ins = Activator.CreateInstance<T>();
                list.Add(ins);
            }
            
            poolTypes.Add(n, c);
        }
        
        static ProtoPool()
        {
            Add<GameResMsg>( nameof(GameResMsg),12);
            Add<HeartBeatMsg>(nameof(HeartBeatMsg),1);
            // Add<ItemMsg>(nameof(ItemMsg),64);
            
            Add<Client2Server>(nameof(Client2Server), 16);
            Add<Server2Client>(nameof(Server2Client), 16);
            
            Add<LobbySyncCharacterPosClient2Gate>(nameof(LobbySyncCharacterPosClient2Gate), 2);
            Add<CitySyncCharacterPosClient2Gate>(nameof(CitySyncCharacterPosClient2Gate), 2);
        }

        public static T Clone<T>(T other) where T : IPoolObject<T>, IMessage, new()
        {
            return ProtoPool<T>.Clone(other);
        }
        
        public static T Get<T>() where T : IPoolObject<T>, IMessage, new()
        {
            return ProtoPool<T>.Get();
        }

        public static T Get<T>(string name) where T : IPoolObject<T>, IMessage, new()
        {
#if UNITY_EDITOR
            msgCounter.TryGetValue(name, out var count);
            msgCounter[name] = count + 1;
#endif
            
            if (poolTypes.TryGetValue(name, out var c))
            {
                if (!Pool.TryGetValue(name, out var list))
                {
                    list = new List<object>(c)
                    {
                        new T(),
                    };

                    for (int i = 1; i < c; ++i)
                    {
                        list.Add(new T());
                    }
                    
                    Pool.Add(name, list);
                }

                if (list.Count > 0)
                {
                    var ret = (T)list[list.Count - 1];
                    list.RemoveAt(list.Count - 1);
                    ret.SetUsed(true);
                    
                    return ret;
                    
                }
            }

            var r = new T();
            r.SetUsed(true);
            return r;
        }
        
        public static void Return<T>(T msg) where T : IPoolObject<T>, IMessage, new()
        {
            ProtoPool<T>.Return(msg);
        }

        public static void Return<T>(string name, T msg) where T : IPoolObject<T>, new()
        {
            var t = name;
            if (poolTypes.ContainsKey(t))
            {
                if (!Pool.TryGetValue(t, out var list))
                {
                    list = new List<object>();
                    Pool.Add(t, list);
                }
                
                if (!msg.IsUsed())
                {
                    //DLog.Error($"==================> 元素已在栈内，不能再次回收:{typeof(T).Name}");
                    return;
                }

                msg.Reset();

                list.Add(msg);
                
                msg.SetUsed(false);
            }
        }
    }
    
    public static class ProtoPool<T> where T : IPoolObject<T>, IMessage, new()
    {
        private static string Name;

        public static T Get() 
        {
            if (Name == null)
            {
                Name = typeof(T).Name;
            }
            
            return ProtoPool.Get<T>(Name);
        }

        public static T Clone(T other)
        {
            if (Name == null)
            {
                Name = typeof(T).Name;
            }

            if (ProtoPool.poolTypes.TryGetValue(Name, out var c))
            {
                var obj = Get();
                obj.CopyFrom(other);

                return obj;
            }

            return other;
        }

        public static void Return(T msg)
        {
            if (msg == null)
            {
                return;
            }

            if (Name == null)
            {
                Name = typeof(T).Name;
            }
            
            ProtoPool.Return(Name, msg);
        }
    }

    public interface IPoolObject<T>
    {
        public void Reset();
        public void CopyFrom(T other);

        public T CoverFrom(object o);

        public void SetUsed(bool u);

        public bool IsUsed();
    }

    public class ProtoArrayPool<T> : ArrayPool<T>
    {
#if UNITY_EDITOR
        private static Dictionary<string, int> aaa = new Dictionary<string, int>();
#endif

        public static ArrayPool<T> pool;
        public static T[] RentEntity(int minimumLength)
        {
#if USE_POOL
            if (pool == null)
            {
                pool = Create(64, 4);
            }
            
#if UNITY_EDITOR
            var na = typeof(T).ToString();
            if (!aaa.ContainsKey(na))
            {
                aaa.Add(na, 1);
            }
            else
            {
                aaa[na] = aaa[na] + 1;
            }
            //DLog.Error($"ProtoArrayPool.Rent:{na}-{aaa[na]}");
#endif
            return pool.Rent(minimumLength);
#endif
            return new T[minimumLength];
        }
        
        public override T[] Rent(int minimumLength)
        {
#if USE_POOL
            if (pool == null)
            {
                //DLog.Error($"ProtoArrayPool.Create:{typeof(T)}");
                pool = Create(32, 4);
            }
            
            return pool.Rent(minimumLength);
#endif
            return new T[minimumLength];
        }

        public override void Return(T[] array, bool clearArray = false)
        {
#if USE_POOL
            pool.Return(array, clearArray);
#endif
        }

        public static void ReturnEntity(T[] array, bool clearArray = false)
        {
#if USE_POOL
#if UNITY_EDITOR
            var na = typeof(T).ToString();
            aaa[na] = aaa[na] - 1;
#endif
            
            //DLog.Error($"ProtoArrayPool.Return:{na}-{aaa[na]}");

            pool.Return(array, clearArray);
#endif
        }
    }
}