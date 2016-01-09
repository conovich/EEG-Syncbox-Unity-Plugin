//TCP communication based off of http://www.codeproject.com/Articles/10649/An-Introduction-to-Socket-Programming-in-NET-using

using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

using System.Threading;

using LitJson;

public class TCPServer : MonoBehaviour {

	//logging
	private string simpleLogFile; //gets set based on the current subject in Awake()
	public Logger_Threading simpleLog;



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
			UnityEngine.Debug.Log("Instance already exists!");
			Destroy(transform.gameObject);
			return;
		}
		_instance = this;


		InitLogging();
	}

	//TODO: move to logger_threading perhaps? *shrug*
	void InitLogging(){
		simpleLogFile = "TextFiles/" + TCP_Config.SubjectName + "Log";
		
		int logFileID = 0;
		string logFileIDString = "000";
		
		while(File.Exists(simpleLog.fileName) || logFileID == 0){
			//TODO: move this function somewhere else...?
			if(logFileID < 10){
				logFileIDString = "00" + logFileID;
			}
			else if (logFileID < 100){
				logFileIDString = "0" + logFileID;
			}
			else{
				logFileIDString = logFileID.ToString();
			}
			
			simpleLog.fileName = simpleLogFile + "_" + logFileIDString + ".txt";
			
			logFileID++;
		}
	}

	void Start(){
		RunServer ();

		StartCoroutine(AlignClocks());
		StartCoroutine(SendPhase());
	}

	//test clock alignment, every x seconds
	IEnumerator AlignClocks(){
		yield return new WaitForSeconds(TCP_Config.numSecondsBeforeAlignment);
		while(true){
			myServer.RequestClockAlignment();
			yield return new WaitForSeconds(10.0f);
		}
	}

	//test encoding phase, every x seconds
	IEnumerator SendPhase(){
		yield return new WaitForSeconds(TCP_Config.numSecondsBeforeAlignment);
		while(true){
			myServer.SendEvent(GameClock.SystemTime_Milliseconds, TCP_Config.EventType.PHASE, "ENCODING", "");
			yield return new WaitForSeconds(10.0f);
		}
	}

	void RunServer () {
		myServer = new ThreadedServer ();
		myServer.Start ();
	}

	void Update(){
		GetInput ();
	}

	void GetInput(){
		/*if(Input.GetKeyDown(KeyCode.M)){
			myServer.SendEvent(GameClock.SystemTime_Milliseconds, TCP_Config.EventType.INFO, "KEYPRESS MESSAGE TEST", "no aux to see here.");
		}*/

		if (Input.GetKeyDown (KeyCode.A)) {
			myServer.SendSimpleJSONEvent(GameClock.SystemTime_Milliseconds, TCP_Config.EventType.SUBJECTID, TCP_Config.SubjectName);

			myServer.SendSessionEvent(GameClock.SystemTime_Milliseconds, TCP_Config.EventType.SESSION, 0, TCP_Config.SessionType.NO_STIM);

			List<string> stateList = new List<string>();
			stateList.Add("one");
			stateList.Add("two");
			stateList.Add("three");
			myServer.SendDefineEvent(GameClock.SystemTime_Milliseconds, TCP_Config.EventType.DEFINE, stateList);

			myServer.SendStateEvent(GameClock.SystemTime_Milliseconds, TCP_Config.EventType.STATE, "ENCODING", false);
		}
	}

	public void Log(long time, TCP_Config.EventType eventType){
		simpleLog.Log(time, eventType.ToString());
		UnityEngine.Debug.Log("Logging!");
	}

	//TODO: MOVE ELSEWHERE.
	public void LogSYNCBOX(long time, bool isOn){
		if(isOn){
			simpleLog.Log(time, "SYNCBOX ON");
		}
		else{
			simpleLog.Log(time, "SYNCBOX OFF");
		}
		UnityEngine.Debug.Log("Logging!");
	}

	bool GetIsConnected(){
		return myServer.isServerConnected;
	}

	public void OnExit(){ //call in scene controller when switching to another scene!
		//if (ExperimentSettings_CoinTask.isLogging) {
			simpleLog.close ();
		//}
	}

	void OnApplicationQuit(){
		myServer.End ();
		UnityEngine.Debug.Log ("Ended server.");

		//if (ExperimentSettings_CoinTask.isLogging) {
			simpleLog.close ();
		//}
	}


}








//THREADED SERVER
public class ThreadedServer : ThreadedJob{
	public bool isRunning = false;

	public bool isServerConnected = false;
	public bool isSynced = false;
	Stopwatch clockAlignmentStopwatch;
	int numClockAlignmentTries = 0;
	const int timeBetweenClockAlignmentTriesMS = 500;//500; //half a second
	const int maxNumClockAlignmentTries = 120; //for a total of 60 seconds of attempted alignment




	public string messagesToSend = "";
	string incompleteMessage = "";

	Socket s;
	TcpListener myList;

	int socketTimeoutMS = 5; //TODO: what should I set this to?
		
	public ThreadedServer(){
		
	}
	
	protected override void ThreadFunction()
	{
		isRunning = true;
		// Do your threaded task. DON'T use the Unity API here
		while (isRunning) {
			if(!isServerConnected){
				InitControlPC();
			}
			TalkToClient();
		}
		CleanupConnections();
	}
	
	void TalkToClient(){
		try {
			if(!isSynced){
				if(numClockAlignmentTries < maxNumClockAlignmentTries){
					CheckClockAlignment();
				}
				else{
					//TODO: what to do if the clocked never synced?!
				}
			}

			//OpenConnections();

			//SEND HEARTBEAT
			//messagesToSend = ""; //uncomment to test solo heartbeat.
			SendHeartbeatPolled();

			CheckForMessages();

			SendMessages();

			UnityEngine.Debug.Log("MAIN LOOP EXECUTED");

			//ECHO TEST
			/*
			if(message != ""){

				EchoMessage(message);

				SendMessage(messagesToSend);
				messagesToSend = "";

			}*/



			//CleanupConnections();
			
		}
		catch (Exception e) {
			UnityEngine.Debug.Log("Connection Error....." + e.StackTrace);
		}  
	}

	void InitControlPC(){

		//connect
		OpenConnections();

		//send name of this experiment
		SendEvent(GameClock.SystemTime_Milliseconds, TCP_Config.EventType.EXPNAME, TCP_Config.ExpName, "");

		//send subject ID
		SendEvent(GameClock.SystemTime_Milliseconds, TCP_Config.EventType.SUBJECTID, TCP_Config.SubjectName, "");

		//align clocks //TODO: SHOULD THIS BE FINISHED BEFORE WE START SENDING HEARTBEATS? -- NO
		RequestClockAlignment();

		//start heartbeat
		StartHeartbeatPoll();
	}

	void OpenConnections(){
		IPAddress ipAd = IPAddress.Parse(TCP_Config.HostIPAddress);

		// use local m/c IP address, and 
		// use the same in the client
		
		/* Initializes the Listener */
		myList = new TcpListener(ipAd,TCP_Config.ConnectionPort);
		
		/* Start Listening at the specified port */        
		myList.Start();
		
		UnityEngine.Debug.Log("The server is running at port" + TCP_Config.ConnectionPort + "...");    
		UnityEngine.Debug.Log("The local End point is  :" + myList.LocalEndpoint );
		UnityEngine.Debug.Log("Waiting for a connection.....");
		
		s = myList.AcceptSocket();
		isServerConnected = true;

		//THIS IS VERY IMPORTANT.
		//WITHOUT THIS, SOCKET WILL HANG ON THINGS LIKE RECEIVING MESSAGES IF THERE ARE NO NEW MESSAGES.
			//...because socket.Receive() is a blocking call.
		s.ReceiveTimeout = socketTimeoutMS;

		UnityEngine.Debug.Log("CONNECTED!");
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
			UnityEngine.Debug.Log("Close Server Error....." + e.StackTrace);
		}  
	}




	//CLOCK ALIGNMENT!
	/*
        Task computer starts the process by sending "ALIGNCLOCK' request.
        Control PC will send a sequence of SYNC messages which are echoed back to it
        When it is complete, the Control PC will send a SYNCED message, which indicates 
        it has completed the clock alignment and it is safe for task computer to proceed 
        to the next step.
		*/
	public void RequestClockAlignment(){

		clockAlignmentStopwatch = new Stopwatch();

		isSynced = false;

		SendEvent(GameClock.SystemTime_Milliseconds, TCP_Config.EventType.ALIGNCLOCK, "", "");
		//SendEvent(0, TCP_Config.EventType.ALIGNCLOCK, "0", ""); //JUST FOR DEBUGGING
		UnityEngine.Debug.Log("REQUESTING ALIGN CLOCK");
        
		clockAlignmentStopwatch.Start();
		numClockAlignmentTries = 0;

	}

	//after x seconds have passed, check if the clocks are aligned yet
	int CheckClockAlignment(){
		if(clockAlignmentStopwatch.ElapsedMilliseconds >= timeBetweenClockAlignmentTriesMS){
			if(isSynced){
				UnityEngine.Debug.Log("Sync Complete");
				clockAlignmentStopwatch.Reset();
				return 0;
			}
			else{ //if not synced yet, wait another .5 seconds
				numClockAlignmentTries++;
				clockAlignmentStopwatch.Reset();
				clockAlignmentStopwatch.Start();
				return -1;
			}
		}
		return -1;
	}









	//MESSAGE SENDING AND RECEIVING

	//send all "messages to send"
	void SendMessages(){
		UnityEngine.Debug.Log("SENDING MESSAGE: " + messagesToSend);
		if(messagesToSend != ""){
			SendMessage(messagesToSend);
			messagesToSend = "";
		}
	}

	//send a single message. don't call this on it's own.
	//should use other methods (EchoMessage, SendEvent, etc.) to add messages to "messagesToSend"
	void SendMessage(string message){
		try{
			ASCIIEncoding asen=new ASCIIEncoding();
			s.Send(asen.GetBytes(message));
			UnityEngine.Debug.Log("\nSent Message: " + message);
		}
		catch (Exception e) {
			UnityEngine.Debug.Log("Send Message Error....." + e.StackTrace);
		}
	}

	void EchoMessage(string message){
		messagesToSend += ("ECHO: " + message);
	}

	public void SendEvent(long systemTime, TCP_Config.EventType eventType, string eventData, string auxData){

		//Format the message
		//(from the python code:) TODO: Change to JSONRPC and add checksum
		string t0 = GameClock.FormatTime(systemTime);
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
		
		messagesToSend += message;

		TCPServer.Instance.Log(systemTime, eventType);
	}





	public void SendSimpleJSONEvent(long systemTime, TCP_Config.EventType eventType, string eventData){
		string t0 = GameClock.FormatTime(systemTime);
		
		string jsonEventString = JsonMessageController.FormatSimpleJSONEvent (t0, eventType.ToString(), eventData);
		
		string formattedMessage = FormatJSONMessage(jsonEventString);
		
		
		UnityEngine.Debug.Log (formattedMessage);

		messagesToSend += formattedMessage;
		
		TCPServer.Instance.Log(systemTime, eventType);
	}

	public void SendSessionEvent(long systemTime, TCP_Config.EventType eventType, int sessionNum, TCP_Config.SessionType sessionType){
		string t0 = GameClock.FormatTime(systemTime);

		string jsonEventString = JsonMessageController.FormatJSONSessionEvent (t0, sessionNum.ToString(), sessionType.ToString());
		
		string formattedMessage = FormatJSONMessage(jsonEventString);

		UnityEngine.Debug.Log (formattedMessage);
		
		messagesToSend += formattedMessage;
		
		TCPServer.Instance.Log(systemTime, eventType);
	}

	public void SendDefineEvent(long systemTime, TCP_Config.EventType eventType, List<string> stateList){
		string t0 = GameClock.FormatTime(systemTime);
		
		string jsonEventString = JsonMessageController.FormatJSONDefineEvent (t0, stateList);
		
		string formattedMessage = FormatJSONMessage(jsonEventString);
		
		UnityEngine.Debug.Log (formattedMessage);
		
		messagesToSend += formattedMessage;
		
		TCPServer.Instance.Log(systemTime, eventType);
	}

	public void SendStateEvent(long systemTime, TCP_Config.EventType eventType, string stateName, bool value){
		string t0 = GameClock.FormatTime(systemTime);
		
		string jsonEventString = JsonMessageController.FormatJSONStateEvent (t0, stateName, value.ToString());
		
		string formattedMessage = FormatJSONMessage(jsonEventString);
		
		UnityEngine.Debug.Log (formattedMessage);
		
		messagesToSend += formattedMessage;
		
		TCPServer.Instance.Log(systemTime, eventType);
	}

	string FormatJSONMessage(string jsonMessage){
		string message = TCP_Config.MSG_START.ToString() + jsonMessage + TCP_Config.MSG_END.ToString();
		return message;
	}




	void CheckForMessages(){
		String message = ReceiveMessageBuffer();
		
		ProcessMessageBuffer(message);
	}

	String ReceiveMessageBuffer(){
		String messageBuffer = "";
		try{

			byte[] b=new byte[100];

			int k=s.Receive(b);
			UnityEngine.Debug.Log("Recieved something!");
			if(k > 0){

				for (int i=0; i<k; i++) {
					messageBuffer += Convert.ToChar(b[i]);
				}
			}
			UnityEngine.Debug.Log (messageBuffer);
		}

		catch (Exception e) {
			UnityEngine.Debug.Log("Receive Message Error....." + e.StackTrace);
		}

		return messageBuffer;
	}




	void DecodeJSONMessage(string jsonMessage){
		JsonReader reader = new JsonReader (jsonMessage);
		
		while (reader.Read ()) {
			
			UnityEngine.Debug.Log (reader.Token);
			UnityEngine.Debug.Log (reader.Value);
			
			switch ( (string)reader.Value ){
				case "SUBJECTID":
					//do nothing
					break;
					
				case "SYNC":
					//Sync received from Control PC
					//Echo SYNC back to Control PC with high precision time so that clocks can be aligned
					SendEvent(GameClock.SystemTime_Milliseconds, TCP_Config.EventType.SYNC, GameClock.SystemTime_MicrosecondsString, "");
					break;
					
				case "SYNCNP":
					//Sync received from Control PC
					//Echo SYNC back to Control PC with high precision time so that clocks can be aligned
					SendEvent(GameClock.SystemTime_Milliseconds, TCP_Config.EventType.SYNCNP, GameClock.SystemTime_MicrosecondsString, "");
					break;
					
				case "SYNCED":
					//Control PC is done with clock alignment
					isSynced = true;
					break;
					
				case "EXIT":
					break;
			}
		}
	}

	/*public void ProcessJSONMessageBuffer(string messageBuffer){
		string jsonData = @"*
            {
                ""SUBJECTID""     : ""R1001P"",
				""SESSION"": {
					""session_number"" : 0, 
					""session_type"" : [ 
						""CLOSED_STIM"", 
						""OPEN_STIM"", 
						""NO_STIM"" 
					] 
				}
            }^";

		ProcessMessageBuffer (jsonData + jsonData);
	}*/
	    










	//should work with JSON now... sort of.
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
				UnityEngine.Debug.Log("SEPARATE MESSAGES " + i + ": " + separateMessages[i]);

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
					case 0:
						t0 = splitMessage [i];
						UnityEngine.Debug.Log ("T0: " + t0);
						break;
					case 1:
						id = splitMessage [i];
						UnityEngine.Debug.Log ("ID: " + id);
						break;
					case 2:
						data = splitMessage [i];
						UnityEngine.Debug.Log ("DATA: " + data);
						break;
					case 3:
						aux = splitMessage [i];
						UnityEngine.Debug.Log ("AUX: " + aux);
						break;
				}
			}

			switch (id) {
				case "ID":
					//do nothing
					break;
				case "SYNC":
					//Sync received from Control PC
					//Echo SYNC back to Control PC with high precision time so that clocks can be aligned
					SendEvent(GameClock.SystemTime_Milliseconds, TCP_Config.EventType.SYNC, GameClock.SystemTime_MicrosecondsString, "");
					break;
				case "SYNCED":
					//Control PC is done with clock alignment
					isSynced = true;
					break;
				case "EXIT":
					//Control PC is exiting. If heartbeat is active, this is a premature abort.

					/*
					if self.isHeartbeat and self.abortCallback:
                        self.disconnect()
                        self.abortCallback(self.clock)
					*/
					
					if(isHeartbeat){
						//TODO: do this. am I supposed to check for a premature abort? does it matter? or just end it?
						End ();
					}
					break;
				default:
					break;
			}
		}

	}


	//HEARTBEAT
	bool isHeartbeat = false;
	bool hasSentFirstHeartbeat = false;
	long firstBeat = 0;
	long nextBeat = 0;
	long lastBeat = 0;
	long intervalMS = 1000;
	long delta = 0; //is this ever used?

	void StartHeartbeatPoll(){
		isHeartbeat = true;
		hasSentFirstHeartbeat = false;
	}

	void StopHeartbeatPoll(){
		isHeartbeat = false;
	}

	void SendHeartbeatPolled(){
		//Send continuous heartbeat events every 'intervalMillis'
		//The computation assures that the average interval between heartbeats will be intervalMillis rather...
		//...than intervalMillis + some amount of computational overhead because it is relative to a fixed t0.

		if(hasSentFirstHeartbeat){
			long t1 = GameClock.SystemTime_Milliseconds;
			if ((t1 - firstBeat) > nextBeat ){
				UnityEngine.Debug.Log("HI HEARTBEAT");
				nextBeat = nextBeat + intervalMS;
				delta = t1 - lastBeat;
				lastBeat = t1;
				SendEvent(lastBeat, TCP_Config.EventType.HEARTBEAT, intervalMS.ToString(), "");
			}
		}
		else {
			UnityEngine.Debug.Log("HI FIRST HEARTBEAT");
			firstBeat = GameClock.SystemTime_Milliseconds;
			lastBeat = firstBeat;
			nextBeat = intervalMS;
			SendEvent(lastBeat, TCP_Config.EventType.HEARTBEAT, intervalMS.ToString(), "");
			hasSentFirstHeartbeat = true;
		}
	}



	//FINISHING/ENDING THE THREAD
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
