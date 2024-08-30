using System;
using UnityEngine;

namespace GameLogic
{
    public class UIBase : MonoBehaviour
    {
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            ScriptGenerator();
        }

        public virtual void ScriptGenerator()
        {
        }

        #region FindChildComponent

        public Transform FindChild(string path)
        {
            return FindChild(rectTransform, path);
        }

        public Transform FindChild(Transform transform, string path)
        {
            var findTrans = transform.Find(path);
            if (findTrans != null)
            {
                return findTrans;
            }

            return null;
        }

        public T FindChildComponent<T>(string path) where T : Component
        {
            return FindChildComponent<T>(rectTransform, path);
        }

        public T FindChildComponent<T>(Transform transform, string path) where T : Component
        {
            var findTrans = transform.Find(path);
            if (findTrans != null)
            {
                return findTrans.gameObject.GetComponent<T>();
            }

            return null;
        }

        #endregion
    }
}