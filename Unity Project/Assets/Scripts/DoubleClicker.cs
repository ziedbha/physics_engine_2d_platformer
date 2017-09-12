using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleClicker {
	const float defaultDeltaTime = 0.5f;

	List<KeyCode> keys;
	float deltaTime = defaultDeltaTime;
	float timePass = 0;

	public DoubleClicker(List<KeyCode> keys, float deltaTime) {
		if (keys.Count > 2 || keys.Count == 0) {
			throw new ArgumentException ("Number of Keys is incorrect");
		}
		this.keys = keys;
		this.deltaTime = deltaTime;
	}

	public DoubleClicker(List<KeyCode> keys) {
		if (keys.Count > 2 || keys.Count == 0) {
			throw new ArgumentException ("Number of Keys is incorrect");
		}
		this.keys = keys;
	}

	public bool DoubleClickCheck() {
		if (timePass > 0) { 
			timePass -= Time.deltaTime; 
		}

		if (keys.Count == 2) { //count is 2
			if (Input.GetKeyDown(keys[0]) || Input.GetKeyDown(keys[1])) {
				if (timePass > 0) { 
					timePass = 0; 
					return true; 
				}
				timePass = deltaTime;
			}			
		} else { //count is 1
			if (Input.GetKeyDown(keys[0])) {
				if (timePass > 0) { 
					timePass = 0; 
					return true; 
				}
				timePass = deltaTime;
			}	
		}
		return false;
	}
}
