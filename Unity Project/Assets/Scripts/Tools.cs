using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tools {
	//If value is between -1 and 1, then if value gets a rounded value depending on range
	public static void SetValueInRange(float value, float limit) {
		if (limit < 0) {
			throw new ArgumentException ("Limit is negative!");
		}
		if (value == 0) {
			return;
		}
		if (value > 0 && value <= limit) {
			value = limit;

		} else if (value >limit && value <= 1) {
			value = 1;
		} else if (value < 0 && value >= -limit) {
			value = -limit;
		} else {
			value = -1;
		}
	}

	//Applies an easing function to a number
	public static float Ease(float x, float easeFactor) {
		if (easeFactor < 0 || easeFactor > 2) {
			throw new ArgumentException ("Ease Factor is not in range!");
		}
		float a = easeFactor + 1;
		return Mathf.Pow (x, a) / (Mathf.Pow (x, a) + Mathf.Pow (1 - x, a));
	}

}
