using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.IO;
using System;

public class Client : MonoBehaviour
{
	public InputField IPInput, PortInput, NickInput;
	string clientName;

    bool socketReady;
    TcpClient socket;
    NetworkStream stream;
	StreamWriter writer;
    StreamReader reader;


	public void ConnectToServer()
	{//연결하는 함수.
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

		try
		{
			socket.Close();
			socket = null;
			writer = null;
			reader = null;
			socketReady = false;
		}
		catch (Exception e) 
		{
			Chat.instance.ShowMessage($"소켓에러 : {e.Message}");
		}
	}
	void Update()
	{
		if (socketReady && stream.DataAvailable) 
		{
			string data = reader.ReadLine();
			if (data != null)
				OnIncomingData(data);
		}
		float v = Input.GetAxis("Vertical");
		float h = Input.GetAxis("Horizontal");
		if(v!=0 || h!=0)
		{
			cube.transform.position += Time.deltaTime * (new Vector3(v, h, 0));
			Vector3  vec = cube.transform.position;
			Send("position :{0},{1},{2}", vec.x, vec.y, vec.z);
		}
	}
	public GameObject cube;
	void OnIncomingData(string data)
	{
		if (data == "%NAME") 
		{
			clientName = NickInput.text == "" ? "Guest" + UnityEngine.Random.Range(1000, 10000) : NickInput.text;
			Send($"&NAME|{clientName}");
			
			return;
		}
		if(data.Contains("position"))
		{
			string vecStr = data.Split(':')[2];
			float x = float.Parse(vecStr.Split(',')[0]);
			float y = float.Parse(vecStr.Split(',')[1]);
			float z = float.Parse(vecStr.Split(',')[2]);

			cube.transform.position = new Vector3(x, y, z);

			return;
		}
		Chat.instance.ShowMessage(data);
	}

	void Send(string type, Vector3 vec)
	{
		if(!socketReady) return;
		writer.WriteLine(type, vec);
	    writer.Flush();
	}
	void Send(string type, float x, float y, float z)
	{
		if(!socketReady) return;
		writer.WriteLine(type, x, y, z);
	    writer.Flush();
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
