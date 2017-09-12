using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(PhysicsController))]
public class EdgeRiderEnemy : MonoBehaviour {
	public Vector2 startsurface = Vector2.zero; //The surface the enemy starts on - (0,1) represents the top, etc
	public float startdirection = 1;
	public float speed = 1;

	float grav = 150;
	bool touchdown = false;
	float velocityXSmoothing;
	float velocityYSmoothing;
	Vector2 gravity;
	Vector2 velocity = Vector2.zero;

	PhysicsController controller;

	void Awake (){
		controller = GetComponent<PhysicsController> ();
	}

	void Start () {
		gravity = new Vector2 (-startsurface.x, -startsurface.y);
		velocity = gravity;

	}

	void FixedUpdate () {
		float x = velocity.x;
		float y = velocity.y;
		bool damp = OnCollide ();
		velocity += gravity * grav * Time.deltaTime;
		if (damp) {
			velocity.x = Mathf.SmoothDamp (x, velocity.x, ref velocityXSmoothing, .05f);
			velocity.y = Mathf.SmoothDamp (y, velocity.y, ref velocityYSmoothing, .05f);
		}
		controller.Move (velocity * Time.deltaTime);
	}

	bool OnCollide() {
		bool collided = false;
		bool damp = true;
		Vector2 prevgravity = new Vector2 (gravity.x, gravity.y);
		if (controller.Above ()) {
			if (!prevgravity.Equals (new Vector2 (0, 1))) {
				gravity.x = 0;
				gravity.y = 1;
				damp = false;
			}
			collided = true;
		} 
		if (controller.Below ()) {
			if (!prevgravity.Equals (new Vector2 (0, -1))) {
				gravity.x = 0;
				gravity.y = -1;
				damp = false;
			}
			collided = true;
		} 
		if (controller.Left ()) {
			if (!prevgravity.Equals (new Vector2 (-1, 0))) {
				gravity.x = -1;
				gravity.y = 0;
				damp = false;
			}
			collided = true;
		} 
		if (controller.Right ()) {
			if (!prevgravity.Equals (new Vector2 (1, 0))) {
				gravity.x = 1;
				gravity.y = 0;
				damp = false;
			}
			collided = true;
		}

		if (collided && !touchdown) {
			touchdown = true;
		}

		Vector2 currDir = Rotate (gravity, -1 * startdirection);
		if (collided) {
			velocity = currDir * speed;
		} else if (touchdown){
			velocity = speed  * gravity;
			gravity = Rotate (gravity, startdirection);
			touchdown = false;
		}
		return damp;
	}
		
	Vector2 Rotate (Vector2 v, float dir){
		Vector2 v2 = Vector2.zero;
		if (dir == 1) {
			v2.y = -v.x;
			v2.x = v.y;
		} else {
			v2.y = v.x;
			v2.x = -v.y;
		}

		return v2;
	}
}
