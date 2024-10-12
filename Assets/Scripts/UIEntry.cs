// using Fantasy;
using Protocol;
using TPFramework;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
	class UIEntry : UIBase
	{
		// 网络正常是在获取版本信息的时候由服务器返回
		// LOCAL : 127.0.0.1:22001
		[SerializeField] private string ip = "";
		[SerializeField] private int port = 0;

		private bool isInited;
		private bool isLoginOK;
		private bool isEnterRoom;
		IPrivateDataHelper dataHelper;
		public enum eGameType
		{
			None = 0,
			Lobby = 1001,
			City = 3001,
		}
		private eGameType gameType;
		private void RegisterMessage()
		{
			if (isInited)
				return;
			
			isInited = true;
			isLoginOK = false;
			isEnterRoom = false;
			
			dataHelper = new PrivateDataHelper();
			
			NetMessageMgr.RegisterMsgHandle(MsgType.PlayerLoginReq, OnPlayerLoginReq);
			NetMessageMgr.RegisterMsgHandle(MsgType.PlayerSwitchMiniGameReq, OnPlayerSwitchMiniGame);
			NetMessageMgr.RegisterMsgHandle(MsgType.SpinLuckyWheelReq, OnPlayerSpinLuckyWheel);
			NetMessageMgr.RegisterMsgHandle(MsgType.PlayerRoblingNtf, OnRoblingSyncNtf);
			NetMessageMgr.RegisterMsgHandle(MsgType.DisconnectNtf, OnDisconnectNtf);
		}

		private void OnDisconnectNtf(NetworkMessage content)
		{
			DLog.Log("OnDisconnectNtf: " + content.mainStatus);
		}

		private void OnRoblingSyncNtf(NetworkMessage content)
		{
			SyncPlayerRoblingNtfGate2Client data = SyncPlayerRoblingNtfGate2Client.Parser.ParseFrom(content.Data);
			DLog.Log("OnRoblingSyncNtf: " + data.ToString());
		}

		private void OnPlayerSpinLuckyWheel(NetworkMessage content)
		{
			if (string.IsNullOrEmpty(content.mainStatus))
			{
				var msg = SpinLuckyWheelGate2Client.Parser.ParseFrom(content.Data);
				DLog.Log("OnPlayerSpinLuckyWheel: " + msg.ToString());

				SendClaimWheelReward();
			}
			else
			{
				DLog.Error("OnPlayerSpinLuckyWheel: " + content.mainStatus);
			}
		}

		private void OnPlayerSwitchMiniGame(NetworkMessage content)
		{
			if (string.IsNullOrEmpty(content.mainStatus))
			{
				var msg = PlayerSwitchMiniGameGate2Client.Parser.ParseFrom(content.Data);
				DLog.Log("OnPlayerSwitchMiniGame, and StartGame: " + msg.ToString());

				gameType = (eGameType) msg.MiniGameTypeConfigId;
			}
			else
			{
				DLog.Error("OnPlayerSwitchMiniGame: " + content.mainStatus);
			}
		}

		private void OnPlayerLoginReq(NetworkMessage content)
		{
			if (!string.IsNullOrEmpty(content.mainStatus))
			{
				DLog.Error($"LoginFailed: {content.mainStatus}");
				return;
			}
			
			var msg = PlayerLoginGate2Client.Parser.ParseFrom(content.Data);
			DLog.Log("OnPlayerLogin OK: " + msg.ToString());
			
			SendLoginOK();
		}

		private void SendLoginOK()
		{
			var msg = new PlayerLoginOKClient2Gate()
			{
				LocalizedName = "CUSTOM_PLAYER_NAME"
			};
			NetMessageMgr.SendMsg(MsgType.PlayerLoginOkReq, msg, (type, content) =>
			{
				if (string.IsNullOrEmpty(content.mainStatus))
				{
					isLoginOK = true;
					
					var data = PlayerLoginGate2Client.Parser.ParseFrom(content.Data);
					DLog.Log("SendLoginOK OK: " + data.ToString());
				}
				else
				{
					isLoginOK = false;
					DLog.Error("SendLoginOK FAILED: " + content.mainStatus);
				}
				return true;
			}, true, null);
		}

		private void SendClaimWheelReward()
		{
			var msg = new ClaimLuckyWheelRewardClient2Gate();
			NetMessageMgr.SendMsg(MsgType.ClaimLuckyWheelRewardReq, msg, (type, content) =>
			{
				if (string.IsNullOrEmpty(content.mainStatus))
				{
					var data = EnterLuckyWheelGate2Client.Parser.ParseFrom(content.Data);
					DLog.Log("SendClaimWheelReward: " + data.ToString());
				}
				else
				{
					DLog.Error("SendClaimWheelReward: " + content.mainStatus);
				}
				return true;
			}, true, null);
		}

		#region 脚本工具生成的代码
		private Button m_btnSendButton;
		private Button m_btnSendRPCButton;
		private Button m_btnReceiveButton;
		private Button m_btnWheelButton;
		private Button m_btnLoginAddressButton;
		private Button m_btnSendAddressButton;
		private Button m_btnSendAddressRPCButton;
		private Button m_btnReceiveAddressButton;
		private Button m_btnConnentServerButton;
		private Button m_btnLoginUIButton;
		private Text m_textMessage;
		
		public override void ScriptGenerator()
		{
			m_btnSendButton = FindChildComponent<Button>("Scroll View/Viewport/UIEntry/m_btnSendButton");
			m_btnSendRPCButton = FindChildComponent<Button>("Scroll View/Viewport/UIEntry/m_btnSendRPCButton");
			m_btnReceiveButton = FindChildComponent<Button>("Scroll View/Viewport/UIEntry/m_btnReceiveButton");
			m_btnWheelButton = FindChildComponent<Button>("Scroll View/Viewport/UIEntry/m_btnWheelButton");
			m_btnLoginAddressButton = FindChildComponent<Button>("Scroll View/Viewport/UIEntry/m_btnLoginAddressButton");
			m_btnSendAddressButton = FindChildComponent<Button>("Scroll View/Viewport/UIEntry/m_btnSendAddressButton");
			m_btnSendAddressRPCButton = FindChildComponent<Button>("Scroll View/Viewport/UIEntry/m_btnSendAddressRPCButton");
			m_btnReceiveAddressButton = FindChildComponent<Button>("Scroll View/Viewport/UIEntry/m_btnReceiveAddressButton");
			m_btnConnentServerButton = FindChildComponent<Button>("Scroll View/Viewport/UIEntry/m_btnConnentServerButton");
			m_btnLoginUIButton = FindChildComponent<Button>("Scroll View/Viewport/UIEntry/m_btnLoginUIButton");
			m_textMessage = FindChildComponent<Text>("Scroll View/Viewport/UIEntry/m_textMessage");
			m_btnSendButton.onClick.AddListener(OnClickSendButtonBtn);
			m_btnSendRPCButton.onClick.AddListener(OnClickSendRPCButtonBtn);
			m_btnReceiveButton.onClick.AddListener(OnClickReceiveButtonBtn);
			m_btnWheelButton.onClick.AddListener(OnClickWheelButtonBtn);
			m_btnLoginAddressButton.onClick.AddListener(OnClickLoginAddressButtonBtn);
			m_btnSendAddressButton.onClick.AddListener(OnClickSendAddressButtonBtn);
			m_btnSendAddressRPCButton.onClick.AddListener(OnClickSendAddressRPCButtonBtn);
			m_btnReceiveAddressButton.onClick.AddListener(OnClickReceiveAddressButtonBtn);
			m_btnConnentServerButton.onClick.AddListener(OnClickConnentServerButtonBtn);
			m_btnLoginUIButton.onClick.AddListener(OnClickLoginUIButtonBtn);
		}
		#endregion

		#region 事件
		private void OnClickSendButtonBtn()
		{
			Log("OnClickSendButtonBtn");
			// NetworkManager.Instance.Session.Send(new C2G_TestMessage()
			// {
			// 	Tag = "OnClickSendButtonBtn"
			// });
			
			// NetMessageMgr.SendCheckConnection();

			var msg = dataHelper.MakePrivateData<PlayerLoginClient2Gate>();
			NetMessageMgr.SendMsg(MsgType.PlayerLoginReq, msg, null, true, null);
		}
		private void OnClickSendRPCButtonBtn()
		{
			Log("OnClickSendRPCButtonBtn");
			// var rsp = await NetworkManager.Instance.Session.Call(new C2G_TestRequest()
			// {
			// 	Tag = "OnClickSendRPCButtonBtn"
			// });
			//
			// if (rsp.ErrorCode != 0)
			// {
			// 	Log("OnClickSendRPCButtonBtn ErrorCode:" + rsp.ErrorCode);
			// 	return;
			// }
			//
			// var content = (G2C_TestResponse)rsp;
			// Log("OnClickSendRPCButtonBtn content:" + content.Tag);

			var msg = new LobbyCharacterEnterRoomClient2Gate();
			NetMessageMgr.SendMsg(MsgType.LobbyCharacterEnterRoomReq, msg, (type, content) =>
			{
				if (string.IsNullOrEmpty(content.mainStatus))
				{
					isEnterRoom = true;
					var data = LobbyCharacterEnterRoomGate2Client.Parser.ParseFrom(content.Data);
					DLog.Log("EnterRoom OK: " + data.ToString());
				}
				else
				{
					isEnterRoom = false;
					DLog.Error("EnterRoom Failed: " + content.mainStatus);
				}
				return true;
			}, true, null);
		}
		private void OnClickReceiveButtonBtn()
		{
			Log("OnClickReceiveButtonBtn");
			// NetworkManager.Instance.Session.Send(new C2G_TestNotifyMessage()
			// {
			// 	Msg = "OnClickReceiveButtonBtn"
			// });
			
			var msg = new PlayerSwitchMiniGameClient2Gate()
			{
				MiniGameTypeConfigId = (int)(gameType == 0 ? eGameType.City : (gameType == eGameType.Lobby ? eGameType.City : eGameType.Lobby))
			};
			NetMessageMgr.SendMsg(MsgType.PlayerSwitchMiniGameReq, msg, null, true, null);
		}
		private void OnClickWheelButtonBtn()
		{
			Log("OnClickWheelButtonBtn");
			// NetworkManager.Instance.Session.Send(new C2G_TestNotifyMessage()
			// {
			// 	Msg = "OnClickReceiveButtonBtn"
			// });

			var msg = dataHelper.MakePrivateData<SpinLuckyWheelClient2Gate>();
			NetMessageMgr.SendMsg(MsgType.SpinLuckyWheelReq, msg, null, true, null);
		}
		
		private void OnClickLoginAddressButtonBtn()
		{
			Log("OnClickLoginAddressButtonBtn");
			// var rsp = await NetworkManager.Instance.Session.Call(new C2G_CreateAddressableRequest()
			// {
			// 	
			// });
			//
			// if (rsp.ErrorCode != 0)
			// {
			// 	Log("OnClickLoginAddressButtonBtn ErrorCode:" + rsp.ErrorCode);
			// 	NetworkManager.Instance.AddressRegisted = false;
			// 	return;
			// }
			//
			// var content = (G2C_CreateAddressableResponse)rsp;
			// Log("OnClickLoginAddressButtonBtn OK");
			//
			// NetworkManager.Instance.AddressRegisted = true;
		}
		private void OnClickSendAddressButtonBtn()
		{
			Log("OnClickSendAddressButtonBtn");
			// NetworkManager.Instance.Session.Send(new C2M_TestMessage()
			// {
			// 	Tag = "OnClickSendAddressButtonBtn"
			// });
		}
		private void OnClickSendAddressRPCButtonBtn()
		{
			Log("OnClickSendAddressRPCButtonBtn");
			// var rsp = await NetworkManager.Instance.Session.Call(new C2M_TestRequest()
			// {
			// 	Tag = "OnClickSendAddressRPCButtonBtn"
			// });
			//
			// if (rsp.ErrorCode != 0)
			// {
			// 	Log($"OnClickSendAddressRPCButtonBtn: {rsp.ErrorCode}");
			// 	return;
			// }
			//
			// var content = (M2C_TestResponse)rsp;
			// Log("OnClickSendAddressRPCButtonBtn content:" + content.Tag);
		}
		private void OnClickReceiveAddressButtonBtn()
		{
			Log("OnClickReceiveAddressButtonBtn");
			// NetworkManager.Instance.Session.Send(new C2M_TestNotifyAddressableMessage()
			// {
			// 	Msg = "OnClickReceiveAddressButtonBtn"
			// });
		}
		private void OnClickConnentServerButtonBtn()
		{
			Log("OnClickConnentServerButtonBtn TODO");

			// NetMain.Instance.Connect("127.0.0.1", 22001);
			if (ip == "" || port == 0)
			{
				ip = dataHelper.GetServerIP();
				port = dataHelper.GetServerPort();
			}
			NetMain.Instance.Connect(ip, port);
		}
		private void OnClickLoginUIButtonBtn()
		{
		}
		#endregion

		public void Log(string message)
		{
			m_textMessage.text = message;
			Debug.Log(message);
		}

		private void Start()
		{
			RegisterMessage();
		}

		private void Update()
		{
			if (NetMain.Instance == null)
				return;
			
			bool isConnect = NetMain.Instance.IsConnected();
			if (!isConnect)
			{
				isLoginOK = false;
				isEnterRoom = false;
			}
			bool isAddressed = NetMain.Instance.AddressRegisted;
			m_btnConnentServerButton.interactable = !isConnect;
			m_btnSendButton.interactable = isConnect;
			m_btnSendRPCButton.interactable = isConnect && isLoginOK;
			m_btnReceiveButton.interactable = isConnect && isLoginOK && isEnterRoom;
			m_btnWheelButton.interactable = isConnect && isLoginOK && isEnterRoom;
			
			m_btnLoginAddressButton.interactable = isConnect && !isAddressed;
			m_btnSendAddressButton.interactable = isConnect && isAddressed;
			m_btnSendAddressRPCButton.interactable = isConnect && isAddressed;
			m_btnReceiveAddressButton.interactable = isConnect && isAddressed;
			// m_btnLoginUIButton.interactable = isConnect;
		}
	}
}
