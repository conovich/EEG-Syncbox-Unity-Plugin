using UnityEngine;
using System.Collections;
using System;

public class GameClock : MonoBehaviour {

	public long GameTime_Milliseconds { get { return GetGameTime(); } }
	public static long SystemTime_Milliseconds { get { return GetSystemClockMilliseconds (); } }

	protected long microseconds = 1;
	long initialSystemClockMilliseconds;

	void Awake(){
		initialSystemClockMilliseconds = GetSystemClockMilliseconds();
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

	}

	long GetGameTime(){
		return GetSystemClockMilliseconds () - initialSystemClockMilliseconds;
	}

	static long GetSystemClockMilliseconds(){
		long tick = DateTime.Now.Ticks;
		//Debug.Log (DateTime.Now.Ticks);
		//Debug.Log (DateTime.Now);
		
		//long seconds = tick / TimeSpan.TicksPerSecond;
		long milliseconds = tick / TimeSpan.TicksPerMillisecond;

		return milliseconds;
	}

}
