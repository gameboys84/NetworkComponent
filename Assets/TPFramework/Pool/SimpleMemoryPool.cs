using System;

namespace TPFramework
{
    public interface ISimpleMemoryClean
    {
        public void Reset();
    }
    
    public class SimpleMemoryPool<T> where T : class, ISimpleMemoryClean, new()
    {
        private long id = 0;
        private T[] itemsArray;
        
        public SimpleMemoryPool(int count = 10)
        {
            itemsArray = new T[count] ;

            int c = count > 5 ? 5 : count;
            for (int i = 0; i < c; ++i)
            {
                itemsArray[i] = new T();
            }
        }

        public T Get(int n = 0)
        {
            for (int i = n; i < itemsArray.Length; ++i)
            {
                if (itemsArray[i] != null)
                {
                    var item = itemsArray[i];
                    itemsArray[i] = null;
                    return item;
                }
            }

            //DLog.Error($"SimpleMemoryPool create {typeof(T).ToString()}");
            return new T();
        }

        public void Return(T obj)
        {
            for (int i = 0; i < itemsArray.Length; ++i)
            {
                if (itemsArray[i] == null)
                {
                    itemsArray[i] = obj;
                    obj.Reset();
                    return;
                }
            }
            
            var tmpArray = new T[(int)(itemsArray.Length * 1.3f)];
            Array.Copy(itemsArray, tmpArray, itemsArray.Length);
            itemsArray = tmpArray;
            
            DLog.Warning($"SMP:{typeof(T)}, Count:{tmpArray.Length}");
        }
    }
}