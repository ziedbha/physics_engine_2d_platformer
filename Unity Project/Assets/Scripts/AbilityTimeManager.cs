using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityTimeManager {
	// For (a, {b,c}) in this dict, a cannot happen if either b or c has happened.
	Dictionary<string, List<Tuple>> conflicts;
	Dictionary<string, float> lastOccurrence;
	Dictionary<string, List<string>> children;

	static float timeout = 30f;

	public AbilityTimeManager() {
		conflicts = new Dictionary<string, List<Tuple>>();
		lastOccurrence = new Dictionary<string, float>();
		children = new Dictionary<string, List<string>>();
	}

	public void AddConflict(string ev1, string priorConflict, float timeout){
		if (!conflicts.ContainsKey (ev1)) {
			conflicts.Add (ev1, new List<Tuple> ());
		}
		conflicts [ev1].Add (new Tuple(priorConflict, timeout));
	}

	public void AddChildEvent(string parent, string child){
		if (!children.ContainsKey (parent)) {
			children.Add (parent, new List<string> ());
		}
		children[parent].Add (child);
	}

	public bool IsLockedOut(string evt){
		foreach (Tuple t in conflicts[evt]) {
			string lockev = t.Event ();
			float timelimit = t.Time ();

			if (HasOccuredWithinTime (lockev, timelimit)) {
				return true;
			}
		}

		return false;
	}

	public bool HasOccuredWithinTime(string evt, float time) {
		if (lastOccurrence.ContainsKey (evt)) {
			return lastOccurrence [evt] <= time;
		}
		return false;
	}

	public float LastOccurrence(string evt){
		if (lastOccurrence.ContainsKey (evt)) {
			return lastOccurrence [evt];
		}
		return timeout;
	}

	public void Update(float step){
		Dictionary <string, float> temp = new Dictionary<string, float> ();
		foreach (string evt in lastOccurrence.Keys){
			temp.Add (evt, 0.0f);
		}
		foreach (string evt in temp.Keys){
			float time = lastOccurrence [evt] + step;
			lastOccurrence.Remove (evt);
			if (time < timeout) {
				lastOccurrence.Add (evt, time);
			}
		}
	}

	// Checks if an event can be legally run this frame after current events.
	// Runs an event this frame.
	public bool Trigger(string evt) {
		bool canRun = true;
		if (conflicts.ContainsKey (evt)) {
			foreach (Tuple t in conflicts[evt]) {
				canRun = canRun && !HasOccuredWithinTime (t.Event (), t.Time ());
			}
		}

		if (canRun) {
			if (children.ContainsKey (evt)) {
				foreach (string childevt in children[evt]) {
					if (!Trigger (childevt)) {
						return false;
					}
				}
			}

			if (lastOccurrence.ContainsKey (evt)) {
				lastOccurrence.Remove (evt);
			}
			lastOccurrence.Add (evt, 0);
			return true;
		}
		return false;
	}

	public class Tuple {
		string str;
		float i;
		public Tuple(string s, float v) {
			str = s;
			i = v;
		}

		public string Event() {
			return str;
		}

		public float Time() {
			return i;
		}

		public void SetTime(float t) {
			i = t;
		}
	}

}
