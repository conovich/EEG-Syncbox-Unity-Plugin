using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using LitJson;
using System;

using System.Text.RegularExpressions;

public class JsonMessageController : MonoBehaviour {

	public class Person
	{
		// C# 3.0 auto-implemented properties
		public string   Name     { get; set; }
		public int      Age      { get; set; }
		public DateTime Birthday { get; set; }
	}

	// Use this for initialization
	void Start () {


		string json = @"
            {
                ""SUBJECTID""     : ""R1001P""
            }";

	}
	
	// Update is called once per frame
	void Update () {
	
	}


	public class MessageEvent{
		public string data { get; set; }
		public string type { get; set; }
		public string time { get; set; }
	}

	//Here's a line of JSON for you:
	//{"data": {"name": "ORIENT", "value": true}, "type": "STATE", "time": 1452273876684}

	public static string FormatSimpleJSONEvent(string time, string eventType, string eventData){
		MessageEvent mEvent = new MessageEvent();
		mEvent.data = eventData;
		mEvent.type = eventType;
		mEvent.time = time;
		
		string jsonEventString = JsonMapper.ToJson (mEvent);

		return jsonEventString;
	}

	public static string FormatJSONSessionEvent(string time, string sessionNum, string sessionType){
		string json = @"
          {
			""data"" : {
				""session_number""   :" + sessionNum + @",
            	""session_type"" :" + sessionType + @"
			},
			""type"" : ""SESSION"",
			""time"" : " + time + @"
          }
        ";

		return json;
	}

	public static string FormatJSONDefineEvent(string time, List<string> stateList){
		string dataListSeparator = @",";

		string json = @"
          {
			""data"" : [";

		for (int i = 0; i < stateList.Count; i++) {
			json = json + stateList[i];
			//add a comma if it's not the last item in the list
			if(i != stateList.Count - 1){
				json += dataListSeparator;
			}
		}

		json += @"
			],
			""type"" : ""DEFINE"",
			""time"" : " + time + @"
          }
        ";

		return json;
	}

	public static string FormatJSONStateEvent(string time, string stateName, string boolValue){
		string json = @"
          {
			""data"" : {
				""statename""   :" + stateName + @",
            	""value"" :" + boolValue + @"
			},
			""type"" : ""STATE"",
			""time"" : " + time + @"
          }
        ";

		return json;
	}
}
