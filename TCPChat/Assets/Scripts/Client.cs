using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.IO;
using System;
using System.Linq;

using JsonClass;


public class Client : MonoBehaviour
{
	public InputField IPInput, PortInput, NickInput;
	string clientName;

    bool socketReady;
    TcpClient socket;
    NetworkStream stream;
	StreamWriter writer;
    StreamReader reader;

	UserInfo myInfo;
	[SerializeField] GameObject BackgroundCanvas;
	[SerializeField] ToggleGroup clientInfoList;
	[SerializeField] GameObject clientInfo;
	public Server host;
	bool amIhost = false;
	const string CMD_transform = "Data:transform";
	const string CMD_pickOne = "Data:memId";
	public void ConnectToServer()
	{	//연결하는 함수.
		// 이미 연결되었다면 함수 무시
		if (socketReady) return;

		// 기본 호스트/ 포트번호
		// string ip = IPInput.text == "" ? "127.0.0.1" : IPInput.text;
		string ip = IPInput.text == "" ? "192.168.0.138" : IPInput.text;
		int port = PortInput.text == "" ? 7777 : int.Parse(PortInput.text);

		// 소켓 생성
		try
		{
			socket = new TcpClient(ip, port);
			stream = socket.GetStream();
			writer = new StreamWriter(stream);
			reader = new StreamReader(stream);
			socketReady = true;
		}
		catch (Exception e) 
		{
			Chat.instance.ShowMessage($"소켓에러 : {e.Message}");
		}
	}
	public void DisconnectToServer()
	{
		if(!socketReady) return;

		CloseSocket();
	}
	private void Awake() {
		host.serverOpen += ()=> {amIhost = true;};
	}
	void Update()
	{
		if (socketReady && stream.DataAvailable) 
		{
			string data = reader.ReadLine();
			if (data != null)
				OnIncomingData(data);
		}
        if (amIhost)
        {

            float v = Input.GetAxis("Vertical");
            float h = Input.GetAxis("Horizontal");
            if (v != 0 || h != 0)
            {
                cube.transform.position += 5 * Time.deltaTime * (new Vector3(h, v, 0));
                Vector3 pos = cube.transform.position;
                Quaternion rot = cube.transform.rotation;
                Vector3 sc = cube.transform.localScale;
                tranformData tData = new tranformData
                {
                    position = pos,
                    rotation = rot,
                    scale = sc
                };

                string jData = JsonUtility.ToJson(tData);
                parametorParser parser = new parametorParser
                {
                    type = CMD_transform,
                    data = jData
                };


                Send(JsonUtility.ToJson(parser));
            }
        }
	}
	public GameObject cube;
	public GameObject capsule;
	void OnIncomingData(string data)
	{
		
        if (data.StartsWith("{"))
        {
            parametorParser param = JsonUtility.FromJson<parametorParser>(data);
			string Str = param.data.Replace("\\", string.Empty);
            switch (param.type)
            {
                case CMD_transform :
                    {
                        tranformData td = JsonUtility.FromJson<tranformData>(Str);

                        capsule.transform.position = td.position;
                        capsule.transform.rotation = td.rotation;
                        capsule.transform.localScale = td.scale;
                        break;
                    }
            }


            return;
		}
		else if(data.EndsWith("연결되었습니다."))
		{
			string[] clientData = data.Split('@');
			string memId = clientData[0];
			data = clientData[1];
			name = clientData[1].Replace("님이 연결되었습니다.", "");

			GameObject user = Instantiate(clientInfo, BackgroundCanvas.transform);
			
			Toggle TGL_user = user.GetComponent<Toggle>();
			TGL_user.isOn = false;
			TGL_user.group = clientInfoList;
			user.name = memId;
			user.GetComponentInChildren<Text>().text = name;
			TGL_user.interactable = amIhost;
			
			
		}
		if (data == "%NAME") 
		{
			//Client 데이터 초기화.
			clientName = NickInput.text == "" ? "Guest" + UnityEngine.Random.Range(1000, 10000) : NickInput.text;
			myInfo = new UserInfo
			{
				name = clientName,
				mem_id = "Member" + UnityEngine.Random.Range(1000, 10000)
			};
			string conData = JsonUtility.ToJson(myInfo);
			Send($"&NAME|{conData}");

			return;
		}

		Chat.instance.ShowMessage(data);
	}
	void PickUser()
	{
		Toggle activated = clientInfoList.ActiveToggles().FirstOrDefault();
		parametorParser param = new parametorParser
		{
			type = CMD_pickOne,
			data = activated.name
		};
		Send(JsonUtility.ToJson(param));
	}
	void Send(string data)
	{
		if (!socketReady) return;

		writer.WriteLine(data);
		writer.Flush();
	}

	public void OnSendButton(InputField SendInput) 
	{
#if (UNITY_EDITOR || UNITY_STANDALONE)
		if (!Input.GetButtonDown("Submit")) return;
		SendInput.ActivateInputField();
#endif
		if (SendInput.text.Trim() == "") return;

		string message = SendInput.text;
		SendInput.text = "";
		Send(message);
	}


	void OnApplicationQuit()
	{
		CloseSocket();
	}

	void CloseSocket()
	{
		if (!socketReady) return;

		writer.Close();
		reader.Close();
		socket.Close();
		socketReady = false;
	}
}
