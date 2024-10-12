using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public interface INull
{
    public bool IsNull();
}

public interface IKey
{
    public Int64 GetKey();
}

/// <summary>
/// 扩展Unity Object
/// </summary>
public static class UnityEngineObjectExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SaveAsPNG(this Texture2D texture, string path)
    {
        var bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
    }

    public static Quaternion ToQuaternion(this Vector4 v)
    {
        return new Quaternion(v.x, v.y, v.z, v.w);
    }
    
    public static Vector4 ToVector4(this Quaternion v)
    {
        return new Vector4(v.x, v.y, v.z, v.w);
    }
    
    public static Quaternion ToQuaternion(this float4 v)
    {
        return new Quaternion(v.x, v.y, v.z, v.w);
    }
    
    public static float4 ToFloat4(this Quaternion v)
    {
        return new float4(v.x, v.y, v.z, v.w);
    }
    
    // public static Quaternion ToQuaternion(this V4 v)
    // {
    //     return new Quaternion(v.x, v.y, v.z, v.w);
    // }
    //
    // public static V4 ToV4(this Quaternion v)
    // {
    //     return new V4(v.x, v.y, v.z, v.w);
    // }
    
    /// <summary>
    /// 获得一个Transform，可选择BFS（广度优先搜索）或者DFS
    /// </summary>
    /// <param name="useBFS">适用于层级较低的内容，避免较深查找</param>
    public static Transform GetChildByName(this Transform parent, string childObjName, bool useBFS = false)
    {
        if (useBFS)
            return GetChildByNameInBFS(parent, childObjName);
        else
            return GetChildByNameInDFS(parent, childObjName);
    }

    public static T GetArrayXYW<T>(this T[] a, int x, int y, int w)
    {
        var idx = x + y * w;
        return a[idx];
    }
    
    public static T GetArrayXYW<T>(this ref NativeArray<T> a, int x, int y, int w) where T : struct
    {
        var idx = x + y * w;

        return a[idx];
    }
    
    public static T GetArrayXYWCheckBounds<T>(this ref NativeArray<T> a, int x, int y, int w) where T : struct
    {
        if (x < 0 || y < 0)
        {
            return default;
        }

        if (x >= w || y >= w)
        {
            return default;
        }
        
        var idx = x + y * w;

        return a[idx];
    }
    
    public static bool GetArrayXYWCheckBounds<T>(this T[] a, int x, int y, int w, out T val)
    {
        if (x < 0 || y < 0 || w < 0)
        {
            val = default(T);
            return false;
        }
        
        var idx = x + y * w;
        if (idx < a.Length)
        {
            val = a[idx];
            return true;
        }
        else
        {
            val = default(T);
            return false;
        }
    }
    
    public static bool GetArrayXYWCheckBounds<T>(this ref NativeArray<T> a, int x, int y, int w, out T val) where T : struct
    {
        if (x < 0 || y < 0 || w < 0)
        {
            val = default(T);
            return false;
        }
        
        var idx = x + y * w;
        if (idx < a.Length)
        {
            val = a[idx];
            return true;
        }
        else
        {
            val = default(T);
            return false;
        }
    }
    
    public static void SetArrayXYW<T>(this T[] a, int x, int y, int w, T val)
    {
        var idx = x + y * w;
        a[idx] = val;
    }
    
    public static void SetArrayXYW<T>(this ref NativeArray<T> a, int x, int y, int w, T val) where T : struct
    {
        if (x < 0 || y < 0)
        {
            return;
        }

        if (x >= w || y >= w)
        {
            return;
        }
        
        var idx = x + y * w;
        a[idx] = val;
    }
    
    public static void Clear(this Array a)
    {
        Array.Clear(a, 0, a.Length);
    }

    public static void Clear<T>(this T[] a)
    {
        Array.Clear(a, 0, a.Length);
    }

    public static bool Contains<T>(this T[] a, Int64 key) where T : IKey
    {
        for (int i = 0; i < a.Length; ++i)
        {
            if (a[i].GetKey() == key)
            {
                return true;
            }
        }

        return false;
    }

    public static ref T Find<T>(this T[] a, Int64 key) where T : IKey
    {
        for (int i = 0; i < a.Length; ++i)
        {
            if (a[i].GetKey() == key)
            {
                return ref a[i];
            }
        }
        
        return ref a[0];
    }

    public static bool ReferenceEqualsNotNull(this object o)
    {
        return !ReferenceEquals(o, null);
    }
    public static bool ReferenceEqualsNull(this object o)
    {
        return ReferenceEquals(o, null);
    }

    public static bool Add<T>(this T[] a, T v) where T : INull
    {
        for (int i = 0; i < a.Length; ++i)
        {
            if (a[i].IsNull())
            {
                a[i] = v;
                return true;
            }
        }

        // DLog.Error("大小不对.....");

        return false;
    }

    public static void Remove<T>(this T[] a, T v)
    {
        for (int i = 0; i < a.Length; ++i)
        {
            if (a[i].Equals(v))
            {
                a[i] = default;
                return;
            }
        }

        // DLog.Error("大小不对.....");
    }

    public static void ContainsKey<T>(this T[] a, T v) where T : INull
    {
        for (int i = 0; i < a.Length; ++i)
        {
            if (a[i].IsNull())
            {
                a[i] = v;
                return;
            }
        }

        // DLog.Error("大小不对.....");
    }

    /// <summary>
    /// 获得一个物体（或者其组件），可采用BFS（或DFS）的查找方法
    /// </summary>
    /// <param name="useBFS">适用于层级较低的内容，避免较深查找</param>
    public static Transform FindDeepChild(this Transform parent, string childObjName, bool useBFS = false)
    {
        Transform childGot;
        if (useBFS)
        {
            childGot = GetChildByNameInBFS(parent, childObjName);
        }
        else
        {
            childGot = GetChildByNameInDFS(parent, childObjName);
        }

        if (childGot == null)
            //Not found
            return null;
        else
            return childGot.GetComponent<Transform>();
    }

    /// <summary>
    /// 采用广度优先的方式查找Transform的子物体（及其后代）中第一个含有该名字的物体。适用于无需太多深度搜索的情况；
    /// </summary>
    private static Transform GetChildByNameInBFS(this Transform parent, string childObjName)
    {
        //深度优先（BFS）找到该名字的Transform 
        //使用一个队列进行遍历 =_=
        System.Collections.Generic.Queue<Transform> stack =
            new System.Collections.Generic.Queue<Transform>();

        //--将parent入栈
        stack.Enqueue(parent);

        Transform tmp;
        //--BFS开始
        while (stack.Count > 0)
        {
            tmp = stack.Dequeue();

            if (tmp.name.Equals(childObjName))
            {
                //Great, got it
                return (Transform)tmp;
            }

            //--push back the children
            for (int i = 0; i < tmp.childCount; i++)
            {
                stack.Enqueue(tmp.GetChild(i));
            }
        }

        //Not found
        return null;
    }

    /// <summary>
    /// DFS查找，获得Transform的子物体（及其后代）中第一个含有该名字的物体。适用于需要深度搜索的情况
    /// </summary>
    /// <param name="childObjName">子物体的名字</param>
    private static Transform GetChildByNameInDFS(this Transform parent, string childObjName)
    {
        //深度优先（DFS）找到该名字的Transform 
        //使用一个栈进行遍历 =_=
        System.Collections.Generic.Stack<Transform> stack =
            new System.Collections.Generic.Stack<Transform>();

        //--将parent入栈
        stack.Push(parent);

        Transform tmp;
        //--DFS开始
        while (stack.Count > 0)
        {
            tmp = stack.Pop();

            if (tmp.name.Equals(childObjName))
            {
                //Great, got it
                return (Transform)tmp;
            }

            //--push back the children
            for (int i = tmp.childCount - 1; i >= 0; i--)
            {
                stack.Push(tmp.GetChild(i));
            }
        }

        return null;
    }

    //Not found
    public static bool IsDestroyed(this Object o)
    {
        return o == null;
    }

    public static T GetOrAddComponent<T>(this Component component)
        where T : Component
    {
        var result = component.gameObject.GetComponent<T>();
        if (result.ReferenceEqualsNull())
        {
            result = component.gameObject.AddComponent<T>();
        }

        return result;
    }

    public static T GetOrAddComponent<T>(this GameObject gameObject)
        where T : Component
    {
        var result = gameObject.GetComponent<T>();
        if (result == null)
        {
            result = gameObject.AddComponent<T>();
        }

        return result;
    }

    public static Transform FindChildComplete(this Transform component, string childName)
    {
        Transform forreturn = null;

        foreach (Transform t in component.GetComponentsInChildren<Transform>())
        {
            if (t.name == childName)
            {
                Debug.Log("得到最终子物体的名字是：" + t.name);
                forreturn = t;
                return t;

            }

        }

        return forreturn;
    }

    public static void Destroy(this GameObject o)
    {
        Object.Destroy(o);
    }
    
    public static void Destroy(this Component o)
    {
        Object.Destroy(o);
    }

    public static void Destroy(this Object o)
    {
        Object.Destroy(o);
    }
    
    public static void ScrollToIndex(this ScrollRect scrollRect,int index)
    {
       var childs = scrollRect.content.GetComponentsInChildren<RectTransform>();
       var viewRect = scrollRect.GetComponent<RectTransform>();
       var contentRect = scrollRect.content;
       float viewRectHeight = viewRect.sizeDelta.y;
       Vector2 targetPos = Vector2.zero;
       
       if (index < childs.Length && index >= 0)
       {
           var childTransfor = childs[index];
           
       }
    }
    
    public static void CopyToClipboard(this string str)
    {
        GUIUtility.systemCopyBuffer = str;
    }
    
    public static String ToLocalized(this string str)
    {
        return str;
        // return StringManager.GetGameText(str);
    }
    
    public static string GetEnumName<T>(this T t) where T : struct, IConvertible
    {
        return Enum.GetName(typeof (T), t);
    }

    public static void SetAllChildrenLayer(this Transform root, int layer)
    {
        var children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (var child in children)
        {
            child.gameObject.layer = layer;
        }
    }

    public static unsafe NativeArray<byte> GetPixelsDataBlock(this Texture2D tex, int x, int y, int width, int height)
    {
        var bitPerPixel = 3;
        
        var texData = tex.GetPixelData<byte>(0);
        var texDataPoint = (byte*)texData.GetUnsafeReadOnlyPtr();
        var blockData =
            new NativeArray<byte>(width * height * 3, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        
        var blockDataPoint = (byte*)blockData.GetUnsafePtr();

        int texWidth = tex.width;
        
        int total = 0;
        
        for (int h = 0; h < height; ++h)
        {
            var idx = x + (y + h) * texWidth;

            var source = texDataPoint + idx * bitPerPixel;

            var destination = blockDataPoint + total;

            UnsafeUtility.MemCpy(destination, source, width * bitPerPixel);

            total += width * bitPerPixel;
        }

        texData.Dispose();

        return blockData;
    }

    ///allocator如果是0 控制权就没有，Unity默认就是那样
    public static unsafe NativeArray<T> AsArrayAndSameSafetyHandler<T>(this NativeList<T> nativeList, Allocator allocator)    
        where T : unmanaged
    {
        var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>
            (nativeList.GetUnsafePtr(), nativeList.Length, allocator);
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        //var safetyHandle  = NativeListUnsafeUtility.GetAtomicSafetyHandle<T>(ref nativeList);
        var safetyHandle = AtomicSafetyHandle.Create();
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safetyHandle);
        
        AtomicSafetyHandle.Release(NativeListUnsafeUtility.GetAtomicSafetyHandle<T>(ref nativeList));
#endif
        

        
        return array;
    }
    
    public static void SafeInvoke(this Action action)
    {
        if (action != null)
        {
            action.Invoke();
        }
    }
    
    public static void SafeClear<T>(this IList<T> list)
    {
        if (list != null)
        {
            list.Clear();
        }
    }
    
    public static void SafeClear<TKey, TValue>(this Dictionary<TKey, TValue> dict)
    {
        if (dict != null)
        {
            dict.Clear();
        }
    }
}