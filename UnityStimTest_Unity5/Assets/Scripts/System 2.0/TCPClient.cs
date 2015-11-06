using UnityEngine;
using System.Collections;

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;

public class TCPClient : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		try {
			TcpClient tcpclnt = new TcpClient();
			Debug.Log("Connecting.....");
			
			tcpclnt.Connect("128.59.87.117",8001);
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
}
