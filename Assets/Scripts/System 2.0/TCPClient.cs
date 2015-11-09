using UnityEngine;
using System.Collections;

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;

public class TCPClient : MonoBehaviour {
	
	ThreadedClient myClient;
	
	// Use this for initialization
	void Start () {
		myClient = new ThreadedClient ();
		myClient.Start ();
	}
	
	void OnApplicationQuit(){
		myClient.End ();
		Debug.Log ("Ended client.");
	}
}

public class ThreadedClient : ThreadedJob{
	public bool isRunning = false;
	
	public ThreadedClient(){
		
	}
	
	protected override void ThreadFunction()
	{
		isRunning = true;
		// Do your threaded task. DON'T use the Unity API here
		while (isRunning) {
			ConnectToServer();
		}
		
	}
	
	void ConnectToServer(){
		try {
			TcpClient tcpclnt = new TcpClient();
			Debug.Log("Connecting.....");
			
			tcpclnt.Connect("169.254.50.2",8001);
			// use the ipaddress as in the server program
			
			Debug.Log("Connected");
			Debug.Log("Enter the string to be transmitted : ");
			
			String str=Console.ReadLine();
			Stream stm = tcpclnt.GetStream();
			
			ASCIIEncoding asen= new ASCIIEncoding();
			byte[] ba=asen.GetBytes(str);
			Debug.Log("Transmitting.....");
			
			stm.Write(ba,0,ba.Length);
			
			byte[] bb=new byte[100];
			int k=stm.Read(bb,0,100);
			
			for (int i=0;i<k;i++)
				Debug.Log(Convert.ToChar(bb[i]));
			
			tcpclnt.Close();
		}
		
		catch (Exception e) {
			Debug.Log("Error..... " + e.StackTrace);
		} 
	}
	
	protected override void OnFinished()
	{
		// This is executed by the Unity main thread when the job is finished
		
	}
	
	public void End(){
		isRunning = false;
	}
}