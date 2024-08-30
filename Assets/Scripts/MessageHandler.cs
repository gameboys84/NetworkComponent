// using Fantasy;
// using GameLogic;
//
// public class G2C_TestNotifyMessage_Handler : Message<G2C_TestNotifyMessage>
// {
//     protected override async FTask Run(Session session, G2C_TestNotifyMessage message)
//     {
//         Utils.Log($"Received TestNotifyMessage from server: {message.Msg}");
//         await FTask.CompletedTask;
//     }
// }
//
// public class M2C_TestNotifyAddressableMessage_Handler : Message<M2C_TestNotifyAddressableMessage>
// {
//     protected override async FTask Run(Session session, M2C_TestNotifyAddressableMessage message)
//     {
//         Utils.Log($"Received TestNotifyAddressableMessage from server: {message.Msg}");
//         await FTask.CompletedTask;
//     }
// }
