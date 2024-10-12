using Protocol;
using TPFramework;
using UnityEngine;

public class HeartBeatComponent : SingletonUnity<HeartBeatComponent>
{
    private const float HEART_BEAT_INTERVAL = 15.0f;
    private float _elapsedTime;
    private bool isRunning;

    public void StartTimer()
    {
        isRunning = true;
        _elapsedTime = 0;
    }
    
    public void StopTimer()
    {
        isRunning = false;
        _elapsedTime = 0;
    }

    private void Update()
    {
        if (isRunning)
        {
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= HEART_BEAT_INTERVAL)
            {
                _elapsedTime = 0;
                OnHeartBeat();
            }
        }
    }

    private void OnHeartBeat()
    {
        HeartBeatMsg msg = new HeartBeatMsg();

        NetMessageMgr.SendMsg(MsgType.HeartBeatReqAck, msg, (type, content) =>
        {
            // DLog.Log($"HeartBeatReqAck: {type}, {content.GetMessageId()}");
            return true;
        }, false, null);
    }
}
