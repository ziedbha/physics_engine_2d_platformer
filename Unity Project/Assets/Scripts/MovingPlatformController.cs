using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MovingPlatformController : RaycastController {

	public LayerMask passengerMask;

	[Header("Speed Settings")]
	public float speed = 0.1f;
	[Range(50.0f, 100.0f)]
	public float downVelocityCap = 50.0f;

	[Header("Waypoint Settings")]
	public Vector2[] localWaypoints = new Vector2[2];
	public float waitTime = 0.5f;
	[Range(0, 2)]
	public float easeFactor = 1.5f;
	public bool cyclic = true;

	int fromWaypointIndex = 0;
	float percentBetweenWaypoints;
	float nextMoveTime;
	Vector2 velocity = Vector2.zero;
	Vector2[] globalWaypoints;
	List<PassengerMovement> passengerMovement; //List of PassengerMovement structs for each passenger of the platform
	Dictionary<Transform, PhysicsController> passengerDictionary = new Dictionary<Transform, PhysicsController>(); //HashMap of Transform -> Controller2D for passengers

	public override void Start () {
		base.Start ();
		globalWaypoints = new Vector2[localWaypoints.Length];
		for (int i = 0; i < localWaypoints.Length; i++) {
			globalWaypoints [i] = localWaypoints [i] + (Vector2) transform.position;
		}
		if (speed == 0.0f) { //speed is never 0 for moving platforms
			speed = 0.01f;
		}
	}
		
	void FixedUpdate () {
		UpdateRaycastOrigins ();
		velocity = CalculatePlatformMovement(); //update the velocity based on the movement vector
		if (velocity != Vector2.zero) {
			CalculatePassengerMovement(velocity); //Check collisions with passengers here, add to list of passengers
			MovePassengers (true); //Do all movement of passengers that move BEFORE the platform moves
			transform.position = (Vector2) transform.position + velocity;
			MovePassengers (false); //Do all movement of passengers that move AFTER the platform moves
		}
	}
		
	//Outputs velocity vector of platform based on waypoints
	Vector2 CalculatePlatformMovement() {
		if (Time.time < nextMoveTime) {
			return Vector2.zero;
		}
		fromWaypointIndex %= globalWaypoints.Length;
		int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;
		float distanceBetweenWaypoints = Vector2.Distance (globalWaypoints [fromWaypointIndex], globalWaypoints [toWaypointIndex]);
		percentBetweenWaypoints += Time.deltaTime * speed/distanceBetweenWaypoints;
		percentBetweenWaypoints = Mathf.Clamp01 (percentBetweenWaypoints);
		float easedPercentBetweenWaypoints = Tools.Ease (percentBetweenWaypoints, easeFactor);
		Vector2 newPos = Vector2.Lerp (globalWaypoints [fromWaypointIndex], globalWaypoints [toWaypointIndex], easedPercentBetweenWaypoints);

		if (percentBetweenWaypoints >= 1) {
			percentBetweenWaypoints = 0;
			fromWaypointIndex ++;

			if (!cyclic) {
				if (fromWaypointIndex >= globalWaypoints.Length-1) {
					fromWaypointIndex = 0;
					System.Array.Reverse(globalWaypoints);
				}
			}
			nextMoveTime = Time.time + waitTime;
		}
		return (newPos - (Vector2) transform.position);
	}

	//Main method to move passengers stored in the passengers list AFTER calculating collisions and creating list in CalculatePassengerMovement
	void MovePassengers(bool beforeMovePlatform) {
		foreach (PassengerMovement passenger in passengerMovement) {
			if (!passengerDictionary.ContainsKey(passenger.transform)) {
				//For optimization reasons
				passengerDictionary.Add(passenger.transform,passenger.transform.GetComponent<PhysicsController>());
			}

			if (passenger.moveBeforePlatform == beforeMovePlatform) { 
				//only move passengers that have a boolean flag of moveBeforePlatform that matches that of the method call
				if ((!passengerDictionary [passenger.transform].dropDown)) {
					Vector2 movement = passenger.velocity;
					//if this platform is a one way platform, then if the passenger ignores oneWay platforms in this frame, then do not move them
					passengerDictionary[passenger.transform].Move(movement, passenger.standingOnPlatform);
				}
			}
		}
	}

	//Calculates how much to push passengers that it collided with
	void CalculatePassengerMovement(Vector2 velocity) {
		float directionX = Mathf.Sign (velocity.x);
		float directionY = Mathf.Sign (velocity.y);

		HashSet<Transform> movedPassengers = new HashSet<Transform> ();
		passengerMovement = new List<PassengerMovement> ();

		//Important RayCasting that allows OneWay Moving platforms to work correctly when moving.
		//Basically casts a penetrating ray inside of the platform that checks if there is an object inside of it. Returns an array of hits (not one hit necessarily)
		RaycastHit2D[] hitInside = new RaycastHit2D[0];
		HashSet<string> insideHits = new HashSet<string> ();
		if (gameObject.layer == LayerMask.NameToLayer("OneWay")) {
			Vector2 rayOriginInside = raycastOrigins.topLeft + new Vector2 (-skinWidth, 0);
			float rayLengthInside = collider2d.bounds.size.x;
			hitInside = Physics2D.RaycastAll(rayOriginInside, Vector2.right, rayLengthInside, passengerMask);
			if (hitInside.Length > 0) {
				for (int j = 0; j < hitInside.Length; j++) {
					//Records all hits in this frame in a hash set. Uses name of object as value.
					if (!insideHits.Contains (hitInside[j].transform.name)) {
						insideHits.Add (hitInside[j].transform.name);
					}
				}
			}
		}

		//Check collisions with passengers vertically
		if (velocity.y != 0) {
			float rayLength = Mathf.Abs (velocity.y) + skinWidth;
			for (int i = 0; i < raycastVerticalCount; i++) {
				//Rays are cast in the direction of y-movement
				Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
				rayOrigin += Vector2.right * (verticalRaySpacing * i);
				RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.up * directionY, rayLength, passengerMask);
				if (hit) {
					if (!insideHits.Contains (hit.transform.name)) {
						//if the hit object is NOT inside the object in this frame, then allow collision detection
						if (!movedPassengers.Contains (hit.transform)) {
							//if not already added to the set of movedPassengers
							movedPassengers.Add (hit.transform);
							float pushX = velocity.x;
							float pushY = velocity.y - (hit.distance - skinWidth) * directionY;

							passengerMovement.Add (new PassengerMovement (hit.transform, new Vector2 (pushX, pushY), directionY == 1, true));
						}								
					}
				}
			}
		}
			
		//Check collisions with passengers horizontally. Only do this if platform is NOT OneWay
		if (gameObject.layer != LayerMask.NameToLayer("OneWay")) {
			if (velocity.x != 0) {
				float rayLength = Mathf.Abs (velocity.x) + skinWidth;
				for (int i = 0; i < raycastHorizontalCount; i ++) {
					Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
					rayOrigin += Vector2.up * (horizontalRaySpacing * i);
					RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

					if (hit) {
						if (!insideHits.Contains (hit.transform.name)) {
							if (!movedPassengers.Contains (hit.transform)) {
								movedPassengers.Add (hit.transform);
								float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
								float pushY = -skinWidth;
								passengerMovement.Add (new PassengerMovement (hit.transform, new Vector2 (pushX, pushY), false, true));
							}
						}
					}
				}
			}			
		}

		//Collision check with passengers on top of platform. Only do this if platform is moving down OR purely horizontal movement
		if (directionY == -1 || velocity.y == 0 && velocity.x != 0) {
			float rayLength = skinWidth * 2;
			for (int i = 0; i < raycastVerticalCount; i ++) {
				Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);
				RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);

				if (hit) {
					if (!insideHits.Contains (hit.transform.name)) {
						if (!movedPassengers.Contains (hit.transform)) {
							movedPassengers.Add (hit.transform);
							float pushX = velocity.x;
							float pushY = ((speed) <= downVelocityCap) ? velocity.y : 0;
							passengerMovement.Add (new PassengerMovement (hit.transform, new Vector2 (pushX, pushY), true, false));
						}
					}
				}
			}
		}
	}

	struct PassengerMovement {
		public Transform transform;
		public Vector2 velocity;
		public bool standingOnPlatform;
		public bool moveBeforePlatform;

		public PassengerMovement(Transform _transform, Vector2 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform) {
			transform = _transform;
			velocity = _velocity;
			standingOnPlatform = _standingOnPlatform;
			moveBeforePlatform = _moveBeforePlatform;
		}
	}

	//Draw out the local waypoints in scene view
	void OnDrawGizmos() {
		if (localWaypoints != null) {
			float size = 0.3f;
			Gizmos.color = Color.red;
			for (int i = 0; i < localWaypoints.Length; i++) {
				Vector2 globalWaypointPosition = (Application.isPlaying) ? globalWaypoints[i] : (localWaypoints [i] + (Vector2) transform.position);
				Gizmos.DrawLine (globalWaypointPosition - (Vector2.up * size), globalWaypointPosition + (Vector2.up * size));
				Gizmos.DrawLine (globalWaypointPosition - (Vector2.right * size), globalWaypointPosition + (Vector2.right * size));
			}
		}
	}

}