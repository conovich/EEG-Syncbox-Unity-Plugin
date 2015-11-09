//TCP communication based off of http://www.codeproject.com/Articles/10649/An-Introduction-to-Socket-Programming-in-NET-using

using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

using System.Threading;

public class TCPServer : MonoBehaviour {

	ThreadedServer myServer;
	bool isConnected { get { return GetIsConnected(); } }



	int QUEUE_SIZE = 20;  //Blocks if the queue is full


	//SINGLETON
	private static TCPServer _instance;
	
	public static TCPServer Instance{
		get{
			return _instance;
		}
	}
	
	void Awake(){
		
		if (_instance != null) {
			Debug.Log("Instance already exists!");
			Destroy(transform.gameObject);
			return;
		}
		_instance = this;
	}

	void Start(){
		RunServer ();
	}

	
	void RunServer () {
		myServer = new ThreadedServer ();
		myServer.Start ();
	}

	bool GetIsConnected(){
		return myServer.isServerConnected;
	}

	void OnApplicationQuit(){
		myServer.End ();
		Debug.Log ("Ended server.");
	}


}















public class ThreadedServer : ThreadedJob{
	public bool isRunning = false;

	public bool isServerConnected = false;
	public bool isSynced = false;


	char MSG_START = '[';
	char MSG_SEPARATOR = '~';
	char MSG_END = ']';

	public ThreadedServer(){

	}

	protected override void ThreadFunction()
	{
		isRunning = true;
		// Do your threaded task. DON'T use the Unity API here
		while (isRunning) {
			ConnectClients();
		}

	}

	void ConnectClients(){
		try {
			isServerConnected = false;

			IPAddress ipAd = IPAddress.Parse("169.254.50.2");
			// use local m/c IP address, and 
			// use the same in the client
			
			/* Initializes the Listener */
			TcpListener myList=new TcpListener(ipAd,8001);
			
			/* Start Listening at the specified port */        
			myList.Start();
			
			Debug.Log("The server is running at port 8001...");    
			Debug.Log("The local End point is  :" + myList.LocalEndpoint );
			Debug.Log("Waiting for a connection.....");

			Socket s = myList.AcceptSocket();
			isServerConnected = true;

			String message = ReceiveMessageBuffer(s);
			SendMessage("String recieved by server.", s);
			ProcessMessageBuffer(message);

			/* clean up */            
			s.Close();
			myList.Stop();
			
		}
		catch (Exception e) {
			Debug.Log("Connection Error....." + e.StackTrace);
		}  
	}

	void CloseServer(){
		try{
			isServerConnected = false;
		}
		catch (Exception e) {
			Debug.Log("Close Server Error....." + e.StackTrace);
		}  
	}

	void SendMessage(string message, Socket s){
		try{
			ASCIIEncoding asen=new ASCIIEncoding();
			s.Send(asen.GetBytes(message));
		}
		catch (Exception e) {
			Debug.Log("Send Message Error....." + e.StackTrace);
			Debug.Log("\nSent Acknowledgement");
		}
	}
	
	String ReceiveMessageBuffer(Socket s){
		String messageBuffer = "";
		try{
			byte[] b=new byte[100];
			int k=s.Receive(b);
			Debug.Log("Recieved...");
			if(k > 0){

				for (int i=0; i<k; i++) {
					messageBuffer += Convert.ToChar(b[i]);
				}
			}
			Debug.Log (messageBuffer);
		}

		catch (Exception e) {
			Debug.Log("Receive Message Error....." + e.StackTrace);
		}

		return messageBuffer;
	}

	void ProcessMessageBuffer(string messageBuffer){
		if (messageBuffer != "") {
			string[] splitBuffer = messageBuffer.Split(MSG_START);


			int numMessages = splitBuffer.Length;

			for(int i = 0; i < numMessages; i++){
				DecodeMessage(splitBuffer[i]);
				Debug.Log("MESSAGE BUFFER " + i + ": " + splitBuffer[i]);
			}
		}
	}

	void DecodeMessage(string message){
		string[] splitMessage = message.Split (new Char [] {MSG_START, MSG_SEPARATOR, MSG_END});

		string t0 = "";
		string id = "";
		string data = "";
		string aux = "";

		for (int i = 0; i < splitMessage.Length; i++) {
			switch (i){
				case 0:
					t0 = splitMessage[i];
					Debug.Log("T0: " + t0);
					break;
				case 1:
					id = splitMessage[i];
					Debug.Log("ID: " + id);
					break;
				case 2:
					data = splitMessage[i];
					Debug.Log("DATA: " + data);
					break;
				case 3:
					aux = splitMessage[i];
					Debug.Log("AUX: " + aux);
					break;
			}
		}

		switch (id) {
			case "ID":
				//do nothing
				break;
			case "SYNC":
				//Sync received from Control PC
				//Exho SYNC back to Control PC with high precision time so that clocks can be aligned
				//TODO: do this.
				break;
			case "SYNCED":
				//Control PC is done with clock alignment
				isSynced = true;
				break;
			case "EXIT":
				//Control PC is exiting. If heartbeat is active, this is a premature abort.
				//TODO: do this.
				break;
			default:
				break;
		}

	}

	protected override void OnFinished()
	{
		// This is executed by the Unity main thread when the job is finished

	}

	public void End(){
		if (isServerConnected) {
			CloseServer ();
		}
		isRunning = false;
	}
}
