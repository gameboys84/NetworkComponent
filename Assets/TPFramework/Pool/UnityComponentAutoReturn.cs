
using UnityEngine;

namespace TPFramework
{
    public class UnityComponentAutoReturn : MonoBehaviour
    {
        private UnityComponentPoolHandler poolHandler;
        public void Set(UnityComponentPoolHandler handler)
        {
            poolHandler = handler;
            enabled = true;
        }

        private void Update()
        {
            poolHandler.autoReturnTime -= Time.deltaTime;
            if (poolHandler.autoReturnTime < 0)
            {
                enabled = false;
                poolHandler.Return();
            }
        }
    }
}