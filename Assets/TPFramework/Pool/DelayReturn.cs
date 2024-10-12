using UnityEngine;

namespace TPFramework
{
    public class DelayReturn : MonoBehaviour
    {
        public float delay;
        public UnityComponentPool pool;
        public Component target;
        void Update()
        {
            delay -= Time.deltaTime;

            if (delay <= 0)
            {
                enabled = false;
                pool.Return(target);
            }
        }

    }
}