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

	void Update(){
		GetInput ();
	}

	void GetInput(){
		if(Input.GetKeyDown(KeyCode.M)){
			myServer.SendEvent(GameClock.SystemTime_Milliseconds, ThreadedServer.EventType.INFO, "KEYPRESS MESSAGE TEST", "no aux to see here.");
		}
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

	string messagesToSend = "";

	Socket s;
	TcpListener myList;


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
			TalkToClient();
		}

	}

	void TalkToClient(){
		try {
			OpenConnections();

			String message = ReceiveMessageBuffer();
			//SendMessage("String recieved by server.");
			SendMessage(messagesToSend);
			messagesToSend = "";
			ProcessMessageBuffer(message);

			CleanupConnections();
			
		}
		catch (Exception e) {
			Debug.Log("Connection Error....." + e.StackTrace);
		}  
	}

	void OpenConnections(){
		IPAddress ipAd = IPAddress.Parse("169.254.50.2");
		// use local m/c IP address, and 
		// use the same in the client
		
		/* Initializes the Listener */
		myList = new TcpListener(ipAd,8001);
		
		/* Start Listening at the specified port */        
		myList.Start();
		
		Debug.Log("The server is running at port 8001...");    
		Debug.Log("The local End point is  :" + myList.LocalEndpoint );
		Debug.Log("Waiting for a connection.....");
		
		s = myList.AcceptSocket();
		isServerConnected = true;
	}
	
	void CleanupConnections(){
		/* clean up */            
		s.Close();
		myList.Stop();
		isServerConnected = false;
	}

	void CloseServer(){
		try{
			isServerConnected = false;
		}
		catch (Exception e) {
			Debug.Log("Close Server Error....." + e.StackTrace);
		}  
	}

	public void SendEvent(long systemTime, EventType eventType, string eventData, string auxData){
        //Format the message
        //(from the python code:) TODO: Change to JSONRPC and add checksum
		string t0 = systemTime.ToString();//TODO: "%020.0f" % systemTime;
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

		messagesToSend += message;

	}

					
	void SendMessage(string message){
		try{
			ASCIIEncoding asen=new ASCIIEncoding();
			s.Send(asen.GetBytes(message));
			Debug.Log("\nSent Message: " + message);
		}
		catch (Exception e) {
			Debug.Log("Send Message Error....." + e.StackTrace);
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
		//TODO: DEAL WITH MESSAGES GETTING CUT IN HALF AND SUCH.
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
