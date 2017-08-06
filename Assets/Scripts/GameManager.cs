using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Magnus magnus = new Magnus (Element.Fire, 1, 1, new List<int> ());
		Debug.Log (magnus.element);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
