using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

using JsonClass;

public class Server : MonoBehaviour
{
    public InputField PortInput;

    List<ServerClient> clients;
    List<ServerClient> disconnectList;

    TcpListener server;
    bool serverStarted;
    Dictionary<string, ServerClient> clientData;
    List<string> clientList;
    public delegate void DelgServer();
    public event DelgServer serverOpen;
	public void ServerCreate()
	{
        clients = new List<ServerClient>();
        disconnectList = new List<ServerClient>();
        clientData = new Dictionary<string, ServerClient>();
        clientList = new List<string>();
        try
        {
            int port = PortInput.text == "" ? 7777 : int.Parse(PortInput.text);
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            StartListening();
            serverStarted = true;
            Chat.instance.ShowMessage($"서버가 {port}에서 시작되었습니다.");
            serverOpen();
        }
        catch (Exception e) 
        {
            Chat.instance.ShowMessage($"Socket error: {e.Message}");
        }
	}
    public void ServerRelease()
    {
        Broadcast($"서버 연결이 끊어졌습니다", clients);
        for(int i = 0 ;i<clients.Count;i++)
        {
            clients[i].tcp.Close();
        }
        clients.Clear();
        disconnectList.Clear();
        server.Stop();
        serverStarted = false;
    }
	void Update()
	{
        if (!serverStarted) return;

        foreach (ServerClient c in clients) 
        {
            // 클라이언트가 여전히 연결되있나?
            if (!IsConnected(c.tcp))
            {
                c.tcp.Close();
                disconnectList.Add(c);
                continue;
            }
            // 클라이언트로부터 체크 메시지를 받는다
            else
            {
                NetworkStream s = c.tcp.GetStream();
                if (s.DataAvailable)
                {
                    string data = new StreamReader(s, true).ReadLine();
                    
                    if (data != null)
                    {
                        OnIncomingData(c, data);
                    }
                }
            }
        }

		for (int i = 0; i < disconnectList.Count - 1; i++)
		{
            Broadcast($"{disconnectList[i].clientName} 연결이 끊어졌습니다", clients);

            clients.Remove(disconnectList[i]);
            disconnectList.RemoveAt(i);
		}
	}

	

	bool IsConnected(TcpClient c)
	{
        try
        {
            if (c != null && c.Client != null && c.Client.Connected)
            {
                if (c.Client.Poll(0, SelectMode.SelectRead))
                    return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);

                return true;
            }
            else
                return false;
        }
        catch 
        {
            return false;
        }
	}

	void StartListening()
	{
        server.BeginAcceptTcpClient(AcceptTcpClient, server);
	}

    void AcceptTcpClient(IAsyncResult ar) 
    {
        TcpListener listener = (TcpListener)ar.AsyncState;
        clients.Add(new ServerClient(listener.EndAcceptTcpClient(ar)));
        StartListening();

        // 메시지를 연결된 모두에게 보냄
        Broadcast("%NAME", new List<ServerClient>() { clients[clients.Count - 1] });
    }


    void OnIncomingData(ServerClient c, string data)
    {
        
        if (data.Contains("&NAME")) 
        {//처음 접속시.
            UserInfo userInfo = JsonUtility.FromJson<UserInfo>(data.Split('|')[1]);
            clientData.Add(userInfo.mem_id, c);
            c.clientName = userInfo.mem_id;
            Broadcast($"{c.clientName}@{userInfo.name}님이 연결되었습니다." , clients);
            clientList.Add(userInfo.name);
            return;
        }
        //제이슨 형식인가 확인.
        if(!data.StartsWith("{") && !data.EndsWith("}")) Broadcast($"{clientData[c].name} : {data}", clients);
        else 
        {
            //특정 인원 선택
            if(data.Contains("CMD_pickOne"))
            {
                parametorParser param = JsonUtility.FromJson<parametorParser>(data);
                
                PickOne("", clientData[param.data]);
                return;
            }

            Broadcast(data, clients);
        }
    }

    void Broadcast(string data, List<ServerClient> cl) 
    {
        foreach (var c in cl) 
        {
            try 
            {
                StreamWriter writer = new StreamWriter(c.tcp.GetStream());
                writer.WriteLine(data);
                writer.Flush();
            }
            catch (Exception e) 
            {
                Chat.instance.ShowMessage($"쓰기 에러 : {e.Message}를 클라이언트에게 {c.clientName}");
            }
        }
    }
    void PickOne(string data, ServerClient client)
    {
        try
        {
            StreamWriter writer = new StreamWriter(client.tcp.GetStream());
            writer.WriteLine(data);
                writer.Flush();
        }
        catch (Exception e)
        {
            Chat.instance.ShowMessage($"쓰기 에러 : {e.Message}를 {client}에게 보내지 못함");
        }
    }
}


public class ServerClient
{
    public TcpClient tcp;
    public string clientName;

    public ServerClient(TcpClient clientSocket) 
    {
        clientName = "Guest";
        tcp = clientSocket;
    }
}
