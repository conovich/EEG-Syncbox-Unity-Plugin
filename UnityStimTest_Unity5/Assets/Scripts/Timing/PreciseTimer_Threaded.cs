using UnityEngine;
using System.Collections;


using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;



//CLASS BASED OFF OF: http://answers.unity3d.com/questions/357033/unity3d-and-c-coroutines-vs-threading.html

public class PreciseTimer_Threaded : MonoBehaviour { //I believe I need this for unity instantiation.
	
	BasicTimer myTimer;
	
	// Use this for initialization
	void Start () {
		myTimer = new BasicTimer ();
		myTimer.Start ();
	}
	
	// Update is called once per frame
	void Update () {
		if (myTimer != null) {
			if(myTimer.Update()){ //update returns true when it's finished!
				// Alternative to the OnFinished callback
				myTimer = null;
			}
		}
	}
	
	void OnApplicationQuit(){
		myTimer.End ();
	}
}

public class BasicTimer : ThreadedJob
{
	public bool isRunning = false;

	Stopwatch myStopwatch;
	StreamWriter myStreamWriter;

	
	public BasicTimer() {
		myStopwatch = new Stopwatch ();
		myStreamWriter = new StreamWriter ("TimerTest.txt", false); //no need to append for this test.
	}
	
	protected override void ThreadFunction()
	{
		isRunning = true;
		// Do your threaded task. DON'T use the Unity API here
		myStopwatch.Start ();
		long pastElapsedMS = 0; //total milliseconds elapsed when we last recorded the elapsed millisecond difference
		long currentElapsedMS = 0; //current total milliseconds elapsed
		while (isRunning) {
			currentElapsedMS = myStopwatch.ElapsedMilliseconds; //OVERFLOW WILL OCCUR AFTER ~596 HOURS.
			float elapsedMSDifference = currentElapsedMS - pastElapsedMS;

			if(elapsedMSDifference >= 10){
				myStreamWriter.Write("\n" + currentElapsedMS.ToString());
				pastElapsedMS = currentElapsedMS;
			}
		}
		
	}


	protected override void OnFinished()
	{
		// This is executed by the Unity main thread when the job is finished
		
	}
	
	public void End(){
		myStreamWriter.Flush ();
		myStreamWriter.Close ();
		isRunning = false;
	}

}
