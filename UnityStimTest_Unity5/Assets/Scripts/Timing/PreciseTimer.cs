using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class PreciseTimer : MonoBehaviour {

	List<long> fixedUpdateTimes;
	List<long> fixedUpdateTimeDifferences;
	long lastUpdateTime = 0;

	float numUpdateTimes = 0;

	void Awake(){
		fixedUpdateTimes = new List<long> ();
		fixedUpdateTimeDifferences = new List<long> ();
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

	}

	void FixedUpdate(){
		if (numUpdateTimes < 100) { //just going to store the first 100 values for debugging purposes
			long currentUpdateTime = GetSystemClockMilliseconds ();
			fixedUpdateTimes.Add (currentUpdateTime);

			if (lastUpdateTime != 0) {
				long difference = currentUpdateTime - lastUpdateTime;
				fixedUpdateTimeDifferences.Add (difference);
			}

			lastUpdateTime = currentUpdateTime;

			numUpdateTimes++;
		} 
		else if (numUpdateTimes == 100) {
			numUpdateTimes = 101;
			for(int i = 0; i < fixedUpdateTimeDifferences.Count; i++){
				Debug.Log(fixedUpdateTimeDifferences[i]);
			}
		}
	}


	long GetSystemClockMilliseconds(){
		long tick = DateTime.Now.Ticks;
		//Debug.Log (DateTime.Now.Ticks);
		//Debug.Log (DateTime.Now);
		
		//long seconds = tick / TimeSpan.TicksPerSecond;
		long milliseconds = tick / TimeSpan.TicksPerMillisecond;
		
		return milliseconds;
	}



}
