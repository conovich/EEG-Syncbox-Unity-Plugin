using UnityEngine;
using System.Collections;

//MAC GAME COMPUTER CONTROL
public class RAMControl : MonoBehaviour {

	string MSG_START = "[";
	string MSG_SEPARATOR = "~";
	string MSG_END = "]";
			
	int QUEUE_SIZE = 20;  //Blocks if the queue is full

	public TCPServer myServer;

	//SINGLETON
	private static RAMControl _instance;
	
	public static RAMControl Instance{
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
		myServer.RunServer ();
	}


	long GetSystemTimeInMicros(){
        //Convenience method to return the system time.
		return GameClock.SystemTime_Milliseconds * 1000;
	}

	long GetSystemTimeInMillis(){
		//Convenience method to return the system time.
		return GameClock.SystemTime_Milliseconds;
	}

}
