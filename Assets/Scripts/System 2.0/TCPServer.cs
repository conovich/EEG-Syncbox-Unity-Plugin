//TCP communication based off of http://www.codeproject.com/Articles/10649/An-Introduction-to-Socket-Programming-in-NET-using

using UnityEngine;
using System.Collections;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

using System.Threading;

public class TCPServer : MonoBehaviour {

	ThreadedServer myServer;
	bool isConnected { get { return GetIsConnected(); } }

	// Use this for initialization
	public void RunServer () {
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

			ReceiveMessage(s);
			SendMessage("String recieved the server.", s);

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
	
	void ReceiveMessage(Socket s){
		try{
			byte[] b=new byte[100];
			int k=s.Receive(b);
			Debug.Log("Recieved...");
			if(k > 0){

				for (int i=0; i<k; i++) {
					Debug.Log (Convert.ToChar (b [i]));
				}
			}
		}

		catch (Exception e) {
			Debug.Log("Receive Message Error....." + e.StackTrace);
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
