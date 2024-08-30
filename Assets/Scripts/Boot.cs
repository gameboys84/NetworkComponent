using GameLogic;
using UnityEngine;

public class Boot : MonoBehaviour
{
    void Start()
    {
        NetworkManager.Instance.Initialize(); // .Coroutine();
    }

}
