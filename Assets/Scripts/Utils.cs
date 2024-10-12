using UnityEngine;

namespace GameLogic
{
    public class Utils : MonoBehaviour
    {
        [SerializeField] private UIEntry uiEntry;
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        
        public static void Log(string message)
        {
            Debug.Log(message);
        }
    }
}