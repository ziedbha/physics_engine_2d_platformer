using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour {
	public delegate bool Predicate();

	Animator anim;
	SpriteRenderer sr;
	Dictionary<string, Predicate> conditions;
	float faceDirection;

	void Awake(){
		anim = GetComponentInChildren<Animator> ();
		sr = GetComponentInChildren<SpriteRenderer> ();
		conditions = new Dictionary<string, Predicate> ();
		faceDirection = 1; //default is facing right
	}

	public void AddCondition(string name, Predicate condition){
		conditions.Add (name, condition);
	}

	public void SetFaceDirection (float dir){
		faceDirection = dir;
	}

	public float FaceDirection () {
		return faceDirection;
	}
	
	// Update is called once per frame
	void LateUpdate () {
		foreach (string an in conditions.Keys) {
			anim.SetBool (an, conditions [an] ());
		}
		sr.flipX = faceDirection == -1;
	}
}
