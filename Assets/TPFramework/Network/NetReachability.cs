using UnityEngine;

namespace TPFramework
{
    public class NetReachability : MonoBehaviour
    {
        static bool reachable = true;
        static bool unstable = false;
        public delegate void OnNetReachabilityChangeCB(bool reachable);
        public static event OnNetReachabilityChangeCB OnNetReachabilityChange;

        public static void Reset()
        {
            reachable = true;
            unstable = false;
        }

        public static void CleanUp()
        {
            OnNetReachabilityChange = null;
        }
        

        public void Update()
        {
            if ((Time.frameCount & 0x1F) == 0)
            {
                bool canReach = true;
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    if (unstable == true)
                        canReach = false;
                    else
                        unstable = true;
                }
                else
                {
                    unstable = false;
                    canReach = true;
                }
                if(canReach != reachable)
                {
                    reachable = canReach;
                    DLog.Log("NetReachability state change to " + reachable);
                    if (OnNetReachabilityChange != null)
                        OnNetReachabilityChange(reachable);
                }
            }
        }
    }
}