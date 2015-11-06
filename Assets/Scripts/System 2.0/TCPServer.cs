using UnityEngine;
using System.Collections;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

using System.Threading;

public class TCPServer : MonoBehaviour {

	ThreadedServer myServer;

	// Use this for initialization
	void Start () {
		myServer = new ThreadedServer ();
		myServer.Start ();
	}

	void OnApplicationQuit(){
		myServer.End ();
		Debug.Log ("Ended server.");
	}
}

public class ThreadedServer : ThreadedJob{
	public bool isRunning = false;

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
			IPAddress ipAd = IPAddress.Parse("128.59.87.117");
			// use local m/c IP address, and 
			// use the same in the client
			
			/* Initializes the Listener */
			TcpListener myList=new TcpListener(ipAd,8001);
			
			/* Start Listening at the specified port */        
			myList.Start();
			
			Debug.Log("The server is running at port 8001...");    
			Debug.Log("The local End point is  :" + 
			          myList.LocalEndpoint );
			Debug.Log("Waiting for a connection.....");

			Socket s = myList.AcceptSocket();
			
			
			
			byte[] b=new byte[100];
			int k=s.Receive(b);
			Debug.Log("Recieved...");
			for (int i=0;i<k;i++)
				Debug.Log(Convert.ToChar(b[i]));
			
			ASCIIEncoding asen=new ASCIIEncoding();
			s.Send(asen.GetBytes("The string was recieved by the server."));
			Debug.Log("\nSent Acknowledgement");
			/* clean up */            
			s.Close();
			myList.Stop();
			
		}
		catch (Exception e) {
			//Console.WriteLine("Error..... " + e.StackTrace);
			Debug.Log("UH OH" + e.StackTrace);
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
