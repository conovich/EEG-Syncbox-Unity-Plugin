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
			SendEvent(GameClock.SystemTime_Milliseconds, TCP_Config.EventType.INFO, "KEYPRESS MESSAGE TEST", "no aux to see here.");
		}
	}

	bool GetIsConnected(){
		return myServer.isServerConnected;
	}

	public void SendEvent(long systemTime, TCP_Config.EventType eventType, string eventData, string auxData){
		//Format the message
		//(from the python code:) TODO: Change to JSONRPC and add checksum
		string t0 = systemTime.ToString();//TODO: "%020.0f" % systemTime;
		string message = TCP_Config.MSG_START + t0 + TCP_Config.MSG_SEPARATOR + "ERROR" + TCP_Config.MSG_END;
		
		if (auxData.Length > 0){
			message = TCP_Config.MSG_START
				+ t0 + TCP_Config.MSG_SEPARATOR
					+ eventType.ToString() + TCP_Config.MSG_SEPARATOR
					+ eventData + TCP_Config.MSG_SEPARATOR
					+ auxData + TCP_Config.MSG_END;
		}
		else if( eventData.Length > 0){
			message = TCP_Config.MSG_START
				+ t0 + TCP_Config.MSG_SEPARATOR
					+ eventType.ToString() + TCP_Config.MSG_SEPARATOR
					+ eventData + TCP_Config.MSG_END;
		}
		else{
			message = TCP_Config.MSG_START
				+ t0 + TCP_Config.MSG_SEPARATOR
					+ eventType.ToString() + TCP_Config.MSG_END;
		}
		
		myServer.messagesToSend += message;
		
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

	public string messagesToSend = "";
	string incompleteMessage = "";

	Socket s;
	TcpListener myList;
		
	public ThreadedServer(){
		
	}
	
	protected override void ThreadFunction()
	{
		isRunning = true;
		// Do your threaded task. DON'T use the Unity API here
		OpenConnections();
		while (isRunning) {
			TalkToClient();
		}
		CleanupConnections();
	}

	void TalkToClient(){
		try {
			//OpenConnections();

			String message = ReceiveMessageBuffer();
			//SendMessage("String recieved by server.");
			ProcessMessageBuffer(message);

			//TESTING THE ECHO TEST. ASSUMING MESSAGE == "Test Message"

			if(message != ""){

				messagesToSend = "ECHO: " + message;

				SendMessage(messagesToSend);
				messagesToSend = "";

			}

			//CleanupConnections();
			
		}
		catch (Exception e) {
			Debug.Log("Connection Error....." + e.StackTrace);
		}  
	}

	void OpenConnections(){
		IPAddress ipAd = IPAddress.Parse(TCP_Config.HostIPAddress);

		// use local m/c IP address, and 
		// use the same in the client
		
		/* Initializes the Listener */
		myList = new TcpListener(ipAd,TCP_Config.ConnectionPort);
		
		/* Start Listening at the specified port */        
		myList.Start();
		
		Debug.Log("The server is running at port" + TCP_Config.ConnectionPort + "...");    
		Debug.Log("The local End point is  :" + myList.LocalEndpoint );
		Debug.Log("Waiting for a connection.....");
		
		s = myList.AcceptSocket();
		isServerConnected = true;

		Debug.Log("CONNECTED!");
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
		//DEALS WITH MESSAGES GETTING CUT IN HALF AND SUCH. TODO: I'm guessing this could be refactored...

		string MSG_START_STRING = TCP_Config.MSG_START.ToString ();
		string MSG_END_STRING = TCP_Config.MSG_END.ToString ();


		if (messageBuffer != "") {
			//string[] splitBuffer = messageBuffer.Split(new char[] {MSG_START}, StringSplitOptions.RemoveEmptyEntries);
			string[] splitBuffer = Regex.Split(messageBuffer, "(\\" + MSG_START_STRING + ")");

			List<String> separateMessages = new List<string>();

			for(int i = 0; i < splitBuffer.Length - 1; i++){
				if(splitBuffer[i] == MSG_START_STRING){
					splitBuffer[i] += splitBuffer[i+1]; //will create duplicate messages...
					separateMessages.Add(splitBuffer[i]);
				}
				else{
					if ( (i == 0) && (splitBuffer[i] != "") ){
						separateMessages.Add(splitBuffer[i]);
					}
				}
			}

			int numMessages = separateMessages.Count;

			for(int i = 0; i < numMessages; i++){
				Debug.Log("SEPARATE MESSAGES " + i + ": " + separateMessages[i]);

				//if it's the first element...
				if(i == 0){
					string firstMessage = separateMessages[i];
					//if it contains a start character...
					if( firstMessage.Contains(MSG_START_STRING) ){

						if( firstMessage.Contains(MSG_END_STRING) ){ //first in buffer, has both start and end characters
							//decode normally! a full message!
							DecodeMessage(firstMessage);
						}
						else{//first in buffer, has only start character --> must be an incomplete message.
							incompleteMessage = firstMessage; //incomplete message gets reset.
						}
					}
					else if( firstMessage.Contains(MSG_END_STRING) ){ //message contains only end character, no start --> must be finishing an incomplete message
						if(incompleteMessage.Contains(MSG_START_STRING)){
							incompleteMessage += firstMessage;
							//DECODE IT!
							DecodeMessage(incompleteMessage);
						}
						incompleteMessage = "";
					}
					else{ //no start or end character, must be the center of an incomplete message
						incompleteMessage += firstMessage;
					}
				}
				//else if it's the last element...
				else if( i == numMessages - 1 ){
					string lastMessage = separateMessages[i];
					if( !lastMessage.Contains(MSG_END_STRING) ){ //no end character, must be an incomplete message
						incompleteMessage += splitBuffer[i];
					}
					else{
						//decode normally!
						DecodeMessage(lastMessage);
					}
				}
				//if it's a middle element to the message buffer, it must be a complete message --> decode normally
				else{
					DecodeMessage(separateMessages[i]);
				}
			}
		}
	}

	void DecodeMessage(string message){
		//...assumes we got here with a message in the correct form...

		//Extract content between [] brackets before splitting. Then just split with the message separator.
		string[] messageContent = Regex.Split(message, ( "\\" + TCP_Config.MSG_START + "(.*?)" + "\\" + TCP_Config.MSG_END ) );//"(\\" + MSG_START.ToString() + ")");

		//string[] splitMessage = message.Split (new Char [] {MSG_START, MSG_SEPARATOR, MSG_END});
		if (messageContent.Length > 2) { 
			string[] splitMessage = messageContent [1].Split ( TCP_Config.MSG_SEPARATOR );

			string t0 = "";
			string id = "";
			string data = "";
			string aux = "";

			for (int i = 0; i < splitMessage.Length; i++) {
				switch (i) {
					//CASES 0 & 5 are EMPTY STRINGS.
					case 0:
						t0 = splitMessage [i];
						Debug.Log ("T0: " + t0);
						break;
					case 1:
						id = splitMessage [i];
						Debug.Log ("ID: " + id);
						break;
					case 2:
						data = splitMessage [i];
						Debug.Log ("DATA: " + data);
						break;
					case 3:
						aux = splitMessage [i];
						Debug.Log ("AUX: " + aux);
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
