using UnityEngine;
using System.Collections;

//a parent class for all log track classes
public abstract class LogTrack : MonoBehaviour {
	//public Experiment_CoinTask exp { get { return Experiment_CoinTask.Instance; } }
	//public Logger_Threading subjectLog { get { return Experiment_CoinTask.Instance.subjectLog; } }
	//public Logger_Threading eegLog { get { return Experiment_CoinTask.Instance.eegLog; } }
	public string separator { get { return Logger_Threading.LogTextSeparator; } }

}
