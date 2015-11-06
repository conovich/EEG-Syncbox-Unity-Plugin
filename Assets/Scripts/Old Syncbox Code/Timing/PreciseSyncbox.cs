using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;



//CLASS BASED OFF OF: http://answers.unity3d.com/questions/357033/unity3d-and-c-coroutines-vs-threading.html

public class PreciseSyncbox : MonoBehaviour { //I believe I need this for unity instantiation.
	
	ThreadedSyncbox mySyncbox;
	
	// Use this for initialization
	void Start () {
		//TODO: OPEN USB

		mySyncbox = new ThreadedSyncbox ();
		mySyncbox.Start ();
	}
	
	// Update is called once per frame
	void Update () {
		if (mySyncbox != null) {

			//TESTING STIMULATION TIMING
			if(Input.GetKeyDown(KeyCode.S)){
				UnityEngine.Debug.Log("Starting stim!");
				StartStim(10, 3); //10MS pulse every second for 3 seconds
			}
			if(Input.GetKeyDown(KeyCode.A)){
				UnityEngine.Debug.Log("Starting sync!");
				StartSync(10, 10); //TODO: get rid of duty cycle? make it always out of one second?
			}


			if(mySyncbox.Update()){ //update returns true when it's finished!
				// Alternative to the OnFinished callback
				mySyncbox = null;
			}
		}
	}

	public void StartStim(int newPulseDurationMS, int durationSeconds){
		mySyncbox.SetStimPulse(newPulseDurationMS, durationSeconds);
	}

	public void StartSync(int newPulseDurationMS, int newDutyCycle){
		mySyncbox.SetSyncPulse (newPulseDurationMS, newDutyCycle);
	}

	public void TurnOffPulse(){
		mySyncbox.SetOff ();
	}

	void OnApplicationQuit(){
		mySyncbox.End ();
	}
}

public class ThreadedSyncbox : ThreadedJob
{

	[DllImport ("ASimplePlugin")]
	private static extern IntPtr OpenUSB();
	[DllImport ("ASimplePlugin")]
	private static extern IntPtr CloseUSB();
	[DllImport ("ASimplePlugin")]
	private static extern IntPtr TurnLEDOn();
	[DllImport ("ASimplePlugin")]
	private static extern IntPtr TurnLEDOff();

	public enum SyncboxMode
	{
		sync,
		stim,
		off
	}

	public SyncboxMode myMode = SyncboxMode.off;

	long stimDurationMS = 0;
	int stimPulseDurationMS = 0;
	int syncPulseDurationMS = 0;
	int duty = 0;
	public bool isRunning = false;
	
	Stopwatch overallStopwatch;
	Stopwatch pulseStopwatch;
	StreamWriter myStreamWriter;

	bool isPulseOn = false;
	
	public ThreadedSyncbox() {
		overallStopwatch = new Stopwatch ();
		pulseStopwatch = new Stopwatch ();
		myStreamWriter = new StreamWriter ("SyncboxTimerTest.txt", false); //no need to append for this test.
	}
	
	protected override void ThreadFunction()
	{
		isRunning = true;
		// Do your threaded task. DON'T use the Unity API here
		//long pastElapsedMS = 0; //total milliseconds elapsed when we last recorded the elapsed millisecond difference
		//long currentElapsedMS = 0; //current total milliseconds elapsed
		while (isRunning) {
			//currentElapsedMS = myStopwatch.ElapsedMilliseconds; //OVERFLOW WILL OCCUR AFTER ~596 HOURS.
			//float elapsedMSDifference = currentElapsedMS - pastElapsedMS;
			
			//if(elapsedMSDifference >= 10){
				//myStreamWriter.Write("\n" + currentElapsedMS.ToString());
				//pastElapsedMS = currentElapsedMS;

			//}

			if(myMode == SyncboxMode.sync){
				UpdateSyncPulse();
			}
			else if(myMode == SyncboxMode.stim){
				UpdateStimPulse();
			}
		}
		
	}

	public void SetOff(){
		myMode = SyncboxMode.off;
	}

	public void SetSyncPulse(int newPulseDurationMS, int newDutyCycle){
		myMode = SyncboxMode.sync;

		duty = newDutyCycle;
		syncPulseDurationMS = newPulseDurationMS;

		overallStopwatch.Reset ();
		overallStopwatch.Start ();

		pulseStopwatch.Reset ();
		pulseStopwatch.Start ();
		//TODO: start pulse here!
		isPulseOn = true;
		myStreamWriter.Write("\nSync Pulse On: 0");

	}

	public void SetStimPulse(int newPulseDurationMS, int durationSeconds){ //duration in milliseconds (MS)
		myMode = SyncboxMode.stim;

		duty = 50; //50% duty cycle
		stimDurationMS = GetMillisecondsFromSeconds((long)durationSeconds);
		stimPulseDurationMS = newPulseDurationMS;

		overallStopwatch.Reset ();
		overallStopwatch.Start ();

		pulseStopwatch.Reset ();
		pulseStopwatch.Start ();
		//TODO: start pulse here!
		isPulseOn = true;
		myStreamWriter.Write("\nStim Pulse On: 0");
	}

	//ex: a 10 ms pulse every second — duty cycle
	void UpdateSyncPulse(){
		int fullCycleTime = 1000;//1000 milliseconds = 1 second //(int)( (100.0f / (float)duty) * (float)syncPulseDurationMS ); //SHOULD FULL CYCLE TIME BE DICTATED BY THE SYNCPULSE DURATION? OR PREDEFINED?
		long currentElapsedMS = pulseStopwatch.ElapsedMilliseconds;
		if (currentElapsedMS > syncPulseDurationMS && isPulseOn && currentElapsedMS < fullCycleTime) {
			//TODO: TURN OFF PULSE
			isPulseOn = false;

			myStreamWriter.Write("\nSync Pulse Off: " + currentElapsedMS.ToString());

		}
		if (currentElapsedMS > fullCycleTime && !isPulseOn) {
			//TODO: TURN ON PULSE
			isPulseOn = true;

			myStreamWriter.Write("\nSync Pulse On: " + currentElapsedMS.ToString());
			//overallStopwatch.Reset(); //reset to zero elapsed seconds
			//overallStopwatch.Start();

			pulseStopwatch.Reset(); //reset to zero elapsed seconds
			pulseStopwatch.Start();
		}
	}

	//ex:  a 1ms pulse, 50% duty, constantly for x# of seconds
	void UpdateStimPulse(){
		if (overallStopwatch.ElapsedMilliseconds < stimDurationMS) {
			int fullCycleTime = (int)( (100.0f / (float)duty) * (float)stimPulseDurationMS );

			long currentElapsedMS = pulseStopwatch.ElapsedMilliseconds;

			//myStreamWriter.Write("\nhello" + stimPulseDurationMS);
			//myStreamWriter.Write("\nelapsed: " + currentElapsedMS);

			if (currentElapsedMS > stimPulseDurationMS && currentElapsedMS < fullCycleTime && isPulseOn) {
				//TODO: TURN OFF PULSE
				isPulseOn = false;

				//myStreamWriter.Write("\n oh heyyyy" + stimPulseDurationMS);
				myStreamWriter.Write("\nSync Pulse Off: " + currentElapsedMS.ToString());
			
			}
			if (currentElapsedMS > fullCycleTime && !isPulseOn) {
				//TODO: TURN ON PULSE
				isPulseOn = true;
			
				myStreamWriter.Write("\nSync Pulse On: " + currentElapsedMS.ToString());
				pulseStopwatch.Reset (); //reset to zero elapsed seconds
				pulseStopwatch.Start ();
			}

		}
		else {
			myStreamWriter.Write("\nstim dur remaining is less than zero: " + overallStopwatch.ElapsedMilliseconds);
			SetOff();
		}
	}

	long GetMillisecondsFromSeconds(long seconds){
		return seconds * 1000;
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
