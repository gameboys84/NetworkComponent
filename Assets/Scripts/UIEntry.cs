// using Fantasy;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
	class UIEntry : UIBase
	{
		#region 脚本工具生成的代码
		private Button m_btnSendButton;
		private Button m_btnSendRPCButton;
		private Button m_btnReceiveButton;
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
		}
		private void OnClickReceiveButtonBtn()
		{
			Log("OnClickReceiveButtonBtn");
			// NetworkManager.Instance.Session.Send(new C2G_TestNotifyMessage()
			// {
			// 	Msg = "OnClickReceiveButtonBtn"
			// });
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
			// NetworkManager.Instance.Connect("127.0.0.1", 20000);
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

		private void Update()
		{
			bool isConnect = NetworkManager.Instance.IsConnected();
			bool isAddressed = NetworkManager.Instance.AddressRegisted;
			m_btnConnentServerButton.interactable = !isConnect;
			m_btnSendButton.interactable = isConnect;
			m_btnSendRPCButton.interactable = isConnect;
			m_btnReceiveButton.interactable = isConnect;
			m_btnLoginAddressButton.interactable = isConnect && !isAddressed;
			m_btnSendAddressButton.interactable = isConnect && isAddressed;
			m_btnSendAddressRPCButton.interactable = isConnect && isAddressed;
			m_btnReceiveAddressButton.interactable = isConnect && isAddressed;
			// m_btnLoginUIButton.interactable = isConnect;
		}
	}
}
