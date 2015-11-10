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

	Socket s;


	char MSG_START = '[';
	char MSG_SEPARATOR = '~';
	char MSG_END = ']';

	public enum EventType {
		SUBJECTID,
		EXPNAME,
		VERSION,
		INFO,
		CONTROL,
		SESSION,
		PRACTICE,
		TRIAL,
		PHASE,
		DISPLAYON,
		DISPLAYOFF,
		HEARTBEAT,
		ALIGNCLOCK,
		ABORT,
		SYTNC,
		SYNCED,
		EXIT
	}
		
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

			s = myList.AcceptSocket();
			isServerConnected = true;

			String message = ReceiveMessageBuffer();
			SendMessage("String recieved by server.");
			ProcessMessageBuffer(message);

			SendEvent(GameClock.SystemTime_Milliseconds, EventType.ALIGNCLOCK, "heyoooo", "nothing aux to see here..."); //TODO: gets sent along with the previous send message call... is this alright?

			/* clean up */            
			s.Close();
			myList.Stop();

			isServerConnected = false;
			
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

	void SendEvent(long systemTime, EventType eventType, string eventData, string auxData){

        //Format the message
        //TODO: Change to JSONRPC and add checksum
		string t0 = "";//TODO: "%020.0f" % systemTime;
		string message = MSG_START + t0 + MSG_SEPARATOR + "ERROR" + MSG_END;
			
		if (auxData.Length > 0){
			message = MSG_START
				+ t0 + MSG_SEPARATOR
				+ eventType.ToString() + MSG_SEPARATOR
				+ eventData + MSG_SEPARATOR
					+ auxData + MSG_END;
		}
		else if( eventData.Length > 0){
			message = MSG_START
				+ t0 + MSG_SEPARATOR
					+ eventType.ToString() + MSG_SEPARATOR
					+ eventData + MSG_END;
		}
		else{
			message = MSG_START
				+ t0 + MSG_SEPARATOR
					+ eventType.ToString() + MSG_END;
		}
		SendMessage (message);
	}

					
	void SendMessage(string message){
			try{
				ASCIIEncoding asen=new ASCIIEncoding();
				s.Send(asen.GetBytes(message));
			}
			catch (Exception e) {
			Debug.Log("Send Message Error....." + e.StackTrace);
			Debug.Log("\nSent Acknowledgement");
		}
	}
	
	String ReceiveMessageBuffer(){
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
			string[] splitBuffer = messageBuffer.Split(new char[] {MSG_START}, StringSplitOptions.RemoveEmptyEntries);


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
