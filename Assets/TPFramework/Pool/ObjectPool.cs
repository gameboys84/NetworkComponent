using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TPFramework
{
    /// <summary>
    /// Bass class of ObjectPool.
    /// </summary>
    public abstract class ObjectPool<T> : IDisposable
        where T : UnityEngine.Component
    {
        bool isDisposed = false;
        Queue<T> q;

#if UNITY_EDITOR
        public string pn = string.Empty;
#endif

        /// <summary>
        /// Limit of instace count.
        /// </summary>
        protected int MaxPoolCount
        {
            get { return int.MaxValue; }
        }

        /// <summary>
        /// Create instance when needed.
        /// </summary>
        protected abstract T CreateInstance(Transform root = null);

        /// <summary>
        /// Called before return to pool, useful for set active object(it is default behavior).
        /// </summary>
        protected virtual void OnBeforeRent(T instance)
        {
            instance.gameObject.SetActive(true);
        }

        /// <summary>
        /// Called before return to pool, useful for set inactive object(it is default behavior).
        /// </summary>
        protected virtual void OnBeforeReturn(T instance)
        {
            instance.gameObject.SetActive(false);
        }

        /// <summary>
        /// Called when clear or disposed, useful for destroy instance or other finalize method.
        /// </summary>
        protected virtual void OnClear(T instance)
        {
            if (instance == null) return;

            var go = instance.gameObject;
            if (go == null) return;
            UnityEngine.Object.Destroy(go);
        }

        /// <summary>
        /// Current pooled object count.
        /// </summary>
        public int Count
        {
            get
            {
                if (q == null) return 0;
                return q.Count;
            }
        }

        private static UInt64 idx;

        /// <summary>
        /// Get instance from pool.
        /// </summary>
        public T Rent(Transform root = null)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("ObjectPool was already disposed.");
            }

            if (q == null)
            {
                q = new Queue<T>();
            }


            bool isNew = q.Count == 0;
            //Check 如果 队列中元素由于某种因素出现问题，重新构建正常队列
            if (q.Count > 0 && q.Peek() == null)
            {
                q = new Queue<T>(q.Where(item => item != null));
            }

            T instance = null;
            if (q.Count > 0)
            {
                instance = q.Dequeue();

                if (!ReferenceEquals(root, null))
                {
                    instance.transform.SetParent(root);
                }
            }
            else
            {
                instance = CreateInstance(root);
            }

#if UNITY_EDITOR
            if (isNew)
            {
                if (instance != null && instance.gameObject != null)
                {
                    instance.gameObject.name = $"{instance.name}_PI_{idx++}";
                }
            }
            else
            {
                if (instance != null && instance.gameObject != null)
                {
                    instance.gameObject.name = $"{instance.name}_PP";
                }
            }
#endif

            OnBeforeRent(instance);

            return instance;
        }

        public virtual void Return(T instance)
        {
            if (isDisposed) DLog.Error("ObjectPool was already disposed.");
            if (instance == null)
            {
                DLog.Error("Return instance is null");
                return;
            }

            if (q == null) q = new Queue<T>();

            if ((q.Count + 1) == MaxPoolCount)
            {
                throw new InvalidOperationException("Reached Max PoolSize");
            }

#if UNITY_EDITOR
            instance.gameObject.name = $"{pn}_ReturnIntoPool";
#endif

            OnBeforeReturn(instance);
            q.Enqueue(instance);
        }

        /// <summary>
        /// Clear pool.
        /// </summary>
        public virtual void Clear(bool callOnBeforeRent = false)
        {
            if (q == null) return;
            while (q.Count != 0)
            {
                var instance = q.Dequeue();
                if (callOnBeforeRent)
                {
                    OnBeforeRent(instance);
                }

                OnClear(instance);
            }
        }

        /// <summary>
        /// Trim pool instances. 
        /// </summary>
        /// <param name="instanceCountRatio">0.0f = clear all ~ 1.0f = live all.</param>
        /// <param name="minSize">Min pool count.</param>
        /// <param name="callOnBeforeRent">If true, call OnBeforeRent before OnClear.</param>
        public void Shrink(float instanceCountRatio, int minSize, bool callOnBeforeRent = false)
        {
            if (q == null) return;

            if (instanceCountRatio <= 0) instanceCountRatio = 0;
            if (instanceCountRatio >= 1.0f) instanceCountRatio = 1.0f;

            var size = (int) (q.Count * instanceCountRatio);
            size = Math.Max(minSize, size);

            while (q.Count > size)
            {
                var instance = q.Dequeue();
                if (callOnBeforeRent)
                {
                    OnBeforeRent(instance);
                }

                OnClear(instance);
            }
        }


        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    Clear(false);
                }

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }

    /// <summary>
    /// Bass class of ObjectPool. If needs asynchronous initialization, use this instead of standard ObjectPool.
    /// </summary>
    public abstract class AsyncObjectPool<T> : IDisposable
        where T : UnityEngine.Component
    {
        bool isDisposed = false;
        Queue<T> q;

        /// <summary>
        /// Limit of instace count.
        /// </summary>
        protected int MaxPoolCount
        {
            get { return int.MaxValue; }
        }

        /// <summary>
        /// Create instance when needed.
        /// </summary>
        protected abstract IObservable<T> CreateInstanceAsync();

        /// <summary>
        /// Called before return to pool, useful for set active object(it is default behavior).
        /// </summary>
        protected virtual void OnBeforeRent(T instance)
        {
            instance.gameObject.SetActive(true);
        }

        /// <summary>
        /// Called before return to pool, useful for set inactive object(it is default behavior).
        /// </summary>
        protected virtual void OnBeforeReturn(T instance)
        {
            instance.gameObject.SetActive(false);
        }

        /// <summary>
        /// Called when clear or disposed, useful for destroy instance or other finalize method.
        /// </summary>
        protected virtual void OnClear(T instance)
        {
            if (instance == null) return;

            var go = instance.gameObject;
            if (go == null) return;
            UnityEngine.Object.Destroy(go);
        }

        /// <summary>
        /// Current pooled object count.
        /// </summary>
        public int Count
        {
            get
            {
                if (q == null) return 0;
                return q.Count;
            }
        }

        /// <summary>
        /// Return instance to pool.
        /// </summary>
        public void Return(T instance)
        {
            if (isDisposed) throw new ObjectDisposedException("ObjectPool was already disposed.");
            if (instance == null) throw new ArgumentNullException("instance");

            if (q == null) q = new Queue<T>();

            if ((q.Count + 1) == MaxPoolCount)
            {
                throw new InvalidOperationException("Reached Max PoolSize");
            }

            OnBeforeReturn(instance);
            q.Enqueue(instance);
        }

        /// <summary>
        /// Trim pool instances. 
        /// </summary>
        /// <param name="instanceCountRatio">0.0f = clear all ~ 1.0f = live all.</param>
        /// <param name="minSize">Min pool count.</param>
        /// <param name="callOnBeforeRent">If true, call OnBeforeRent before OnClear.</param>
        public void Shrink(float instanceCountRatio, int minSize, bool callOnBeforeRent = false)
        {
            if (q == null) return;

            if (instanceCountRatio <= 0) instanceCountRatio = 0;
            if (instanceCountRatio >= 1.0f) instanceCountRatio = 1.0f;

            var size = (int) (q.Count * instanceCountRatio);
            size = Math.Max(minSize, size);

            while (q.Count > size)
            {
                var instance = q.Dequeue();
                if (callOnBeforeRent)
                {
                    OnBeforeRent(instance);
                }

                OnClear(instance);
            }
        }

        /// <summary>
        /// Clear pool.
        /// </summary>
        public void Clear(bool callOnBeforeRent = false)
        {
            if (q == null) return;
            while (q.Count != 0)
            {
                var instance = q.Dequeue();
                if (callOnBeforeRent)
                {
                    OnBeforeRent(instance);
                }

                OnClear(instance);
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    Clear(false);
                }

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}