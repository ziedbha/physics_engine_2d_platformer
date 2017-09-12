using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsController : RaycastController {
	public float maxSlopeAngle = 60f; //maximum slope at which object can climb and descend manually

	internal Vector2 currMoveAdjusted; //current move distance adjusted for collision
	internal Vector2 currMoveUnadjusted; //current move distance received from Move() as an argument (unadjusted for collisions)
	internal Vector2 prevMoveAdjusted; //previous physics frame move distance adjusted for collision
	internal Vector2 prevMoveUnadjusted; //previous physics frame move distance received from Move() as an argument (unadjusted for collisions)
	internal float climbSpeedFactor; //% of movement speed applied to climbing speed
	internal bool dropDown; //is object allowed to go down through oneWay platforms this frame?
	float timeUntilResetCollider = -1f;

	internal bool ignoreVertical = false;
	internal bool ignoreHorizontal = false;
	internal Vector2 directionalInput; //reference to player input
	internal CollisionInfo collisions; //stores information about collision detection (see below for struct definition)

	public override void Start () {
		base.Start ();
		collisions.colliderName = "";
		collisions.colliderTag = "";
	}

	//Given a movement distance vector, this method adjusts it for collision, slopes. You can consider this the main collision detection method!
	public void Move(Vector2 moveDistance, bool standingOnPlatform = false) {
		prevMoveAdjusted = currMoveAdjusted;
		currMoveUnadjusted = moveDistance;

		UpdateRaycastOrigins ();
		collisions.Reset ();
		ColliderNamesReset (); //Reset the name/tag of object collided with this frame if needed

		//check if descending slope only if object is not going up
		if (moveDistance.y < 0) { 
			DescendSlopeManual (ref moveDistance);
		}

		//check for automatic slope descent only if previous physics frame allows for it
		if (collisions.descendingSlopeAutoPrev) {
			DescendSlopeAuto (ref moveDistance);
		}
			
		//check horizontal collisions in direction of x-movement
		if (moveDistance.x != 0) { 
			HorizontalCollisions (ref moveDistance);
		}

		//check vertical collisions in direction of y-movement
		if (moveDistance.y != 0) { 
			VerticalCollisions (ref moveDistance);
		}

		//apply final moveDistance vector to object
		transform.Translate (moveDistance);
		Debug.DrawLine ((Vector2) transform.position, (Vector2) transform.position + (moveDistance * 100));

		if (standingOnPlatform) { //for moving platforms only
			collisions.below = true;
		}

		currMoveAdjusted = moveDistance;
		prevMoveUnadjusted = currMoveUnadjusted;
	}
		
	//Checks collisions horizontally. Also checks for climbing slopes
	void HorizontalCollisions (ref Vector2 moveDistance) {
		float directionX = Mathf.Sign (moveDistance.x); //direction of x-movement (-1 is left, 1 is right)
		float rayLength = Mathf.Abs (moveDistance.x) + skinWidth; //raylength starts at the magnitude of moveDistance + offset

		for (int i = 0; i < raycastHorizontalCount; i++) { //for each ray
			Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight; //get origin of ray depending on x-direction
			rayOrigin += Vector2.up * (horizontalRaySpacing * i); //next ray is a bit upwards
			RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right * directionX, rayLength, obstacleCollisionMask); //cast actual ray

			if (hit) {
				float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);
				//CLIMBING SLOPES HERE
				if (i == 0) { //bottom-most ray only
					collisions.colliderTag = hit.collider.tag;
					collisions.colliderName = hit.collider.gameObject.name;
					if (slopeAngle <= maxSlopeAngle) { //if slope is climbable
						if (collisions.descendingSlopeManual) { //if currently descending slope, stop descending and start ascending new one
						collisions.descendingSlopeManual = false;
							moveDistance = prevMoveAdjusted;
						}
						float distanceToSlopeStart = 0; //make sure to reach slope to start climbing
						if (slopeAngle != collisions.slopeAnglePrev) {
							distanceToSlopeStart = hit.distance - skinWidth;
							moveDistance.x -= distanceToSlopeStart * directionX;
						}
						ClimbSlope (ref moveDistance, slopeAngle);
						moveDistance.x += distanceToSlopeStart * directionX;
					}
				}

				//HORIZONTAL COLLISION CHECK
				if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle) { //not climbing a slope or wall in front of object, then check for collisions
					if (this.tag.Equals("Player") && hit.transform.tag.Equals("DestroyOnDash") && ignoreHorizontal) {
						break;
					}
					moveDistance.x = Mathf.Min(Mathf.Abs(moveDistance.x), (hit.distance - skinWidth)) * directionX;
					rayLength = Mathf.Min(Mathf.Abs(moveDistance.x) + skinWidth, hit.distance);
					if (collisions.climbingSlope) { //this happens when wall in front of object and object is climbing a slope
						moveDistance.y = Mathf.Tan(collisions.slopeAngleCurr * Mathf.Deg2Rad) * Mathf.Abs(moveDistance.x); //adjust moveDistance upwards
					}
					collisions.left = directionX == -1;
					collisions.right = directionX == 1;
				}
			}
		}
	}

	//Checks collisions vertically
	void VerticalCollisions (ref Vector2 moveDistance) {
		float directionY = Mathf.Sign (moveDistance.y); //direction of x-movement (-1 is down, 1 is up)
		float rayLength = Mathf.Abs (moveDistance.y) + skinWidth;

		for (int i = 0; i < raycastVerticalCount; i++) { //similar raycast logic
			Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (verticalRaySpacing * i + moveDistance.x);
			RaycastHit2D hitObstacle = Physics2D.Raycast (rayOrigin, Vector2.up * directionY, rayLength, obstacleCollisionMask);
			RaycastHit2D hitOneWay = Physics2D.Raycast (rayOrigin, Vector2.up * directionY, rayLength, oneWayCollisionMask);
			//Collision check for OneWay platforms
			if (directionY == -1 && 
				((hitOneWay && !hitObstacle) || (hitOneWay && hitObstacle && hitOneWay.distance < hitObstacle.distance)) && 
				(collider2d.bounds.min.y >= hitOneWay.collider.bounds.max.y - skinWidth) && !dropDown) {

				float slopeAngle = Vector2.Angle(hitOneWay.normal,Vector2.up);
				moveDistance.y = (hitOneWay.distance - skinWidth) * directionY; //immediately adjust y-moveDistance to stop at collision
				rayLength = hitOneWay.distance;
				if (collisions.climbingSlope) { //if climbing, need to adjust x-moveDistance too
					moveDistance.x = moveDistance.y / Mathf.Tan (collisions.slopeAngleCurr * Mathf.Deg2Rad) * Mathf.Sign (moveDistance.x);
				}
				if (slopeAngle <= maxSlopeAngle) { //steep slopes are NOT considered ground
					collisions.below = true;
				} else { //steep slope is detected, update collisions struct
					collisions.descendingSlopeAutoCurr = true;
					collisions.autoDescentDirectionCurr = Mathf.Sign(hitOneWay.normal.x);
				}
				collisions.onOneWayCurr = true;
			} else if (hitObstacle) { //Collision check for Obstacles
				if (this.tag.Equals("Player") && hitObstacle.transform.tag.Equals("DestroyOnGroundPound") && ignoreVertical && directionY == -1) {
					break; //ignore collision if the physics object is the player and they ignore collisions with this obstacle
				}
				float slopeAngle = Vector2.Angle(hitObstacle.normal, Vector2.up);
				moveDistance.y = (hitObstacle.distance - skinWidth) * directionY; //immediately adjust y-moveDistance to stop at collision
				rayLength = hitObstacle.distance;
				if (collisions.climbingSlope) { //if climbing, need to adjust x-moveDistance too
					moveDistance.x = moveDistance.y / Mathf.Tan (collisions.slopeAngleCurr * Mathf.Deg2Rad) * Mathf.Sign (moveDistance.x);
				}
				if (slopeAngle <= maxSlopeAngle) { //steep slopes are NOT considered ground
					collisions.below = directionY == -1;
				} else if (hitObstacle.normal.y >= 0) { //steep slope is detected, update collisions struct
					collisions.descendingSlopeAutoCurr = true;
					collisions.autoDescentDirectionCurr = Mathf.Sign(hitObstacle.normal.x);
				}
				collisions.above = directionY == 1;
			}
		}

		//Another horizontal raycast to check for horizontal collision while going up slope
		if (collisions.climbingSlope) {
			float directionX = Mathf.Sign(moveDistance.x);
			rayLength = Mathf.Abs(moveDistance.x) + skinWidth;
			Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * moveDistance.y;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, obstacleCollisionMask);

			if (hit) {
				float slopeAngle = Vector2.Angle(hit.normal,Vector2.up);
				if (slopeAngle != collisions.slopeAngleCurr) {
					moveDistance.x = (hit.distance - skinWidth) * directionX;
					collisions.slopeAngleCurr = slopeAngle;
				}
			}
		}
	}

	//Adjusts movement vector and updates collisions struct to climb a climbable slope
	void ClimbSlope(ref Vector2 moveDistance, float slopeAngle) {
		float targetDistance = Mathf.Abs (moveDistance.x) * climbSpeedFactor; //movement distance changes because of slope
		float climbDistanceY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * targetDistance;
		if (moveDistance.y <= climbDistanceY) { //trigonometry to get right x and y distances
			float directionX = Mathf.Sign (moveDistance.x);
			moveDistance.y = climbDistanceY;
			moveDistance.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * targetDistance * directionX;
			collisions.below = true;
			collisions.climbingSlope = true;
			collisions.slopeAngleCurr = slopeAngle;
		}
	}

	//Adjusts movement vector and updates collisions struct to manually descend a climbable slope
	void DescendSlopeManual(ref Vector2 moveDistance) {
		float directionX = Mathf.Sign (moveDistance.x);
		Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
		RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.down, Mathf.Infinity, obstacleCollisionMask);
		if (hit) {
			float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);
			if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle) {
				if (Mathf.Sign(hit.normal.x) == directionX) {
					if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveDistance.x)) {
						float targetDistance = Mathf.Abs (moveDistance.x);
						float descendDistanceY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * targetDistance;
						moveDistance.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * targetDistance * directionX;
						moveDistance.y -= descendDistanceY;
						collisions.slopeAngleCurr = slopeAngle;
						collisions.descendingSlopeManual = true;
						collisions.below = true;
					}					
				}	
			} 
		}
	}

	//Adjusts movement vector and updates collisions struct to automatically descend a non-climbable slope ONLY if there is no correct input
	void DescendSlopeAuto(ref Vector2 moveDistance) {
		//Set up input here
		float inputDirectionX = 0; 
		if (directionalInput != null) { //only get input if it exists
			inputDirectionX = Mathf.Sign (directionalInput.x);
		}
		//pick type of ray origin first
		Vector2 rayOrigin = Vector2.zero;
		if (collisions.autoDescentDirectionPrev == 1) {
			//if slope facing right then rayOrigin is bottom left
			rayOrigin = raycastOrigins.bottomLeft;
		} else if (collisions.autoDescentDirectionPrev == -1) {
			//if slope facing left then rayOrigin is bottom right
			rayOrigin = raycastOrigins.bottomRight;
		}
		float rayLength = Mathf.Abs (moveDistance.y);
		RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.down, rayLength, obstacleCollisionMask);
		if (hit) {
			float descentDirection = Mathf.Sign (hit.normal.x);
			float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);
			float targetDistance = Mathf.Abs (moveDistance.y);
			float descendDistanceY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * targetDistance;
			if (directionalInput != null && directionalInput.x != 0 && inputDirectionX == descentDirection) { //if input is away from slope, prioritize this input
				moveDistance.x = Mathf.Max(Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * targetDistance * descentDirection, moveDistance.x);
			} else { //else use autodescent
				moveDistance.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * targetDistance * descentDirection;
				moveDistance.y -= descendDistanceY;
			}
			collisions.slopeAngleCurr = slopeAngle;
			collisions.descendingSlopeAutoCurr = false;
			collisions.autoDescentDirectionCurr = 0;
		}
	}
		
	//Stores information related to collision detection, slopes...
	public struct CollisionInfo {
		public string colliderTag; //tag of latest collider hit HORIZONTALLY
		public string colliderName; //name of latest collider hit HORIZONTALLY

		public bool above, below, left, right; //directional collision flags
		public bool climbingSlope, descendingSlopeManual; //is object climbing a slope, descending a slope manually

		public bool onOneWayCurr, onOneWayPrev; //is object on a oneway platform this physics frame? Previous physics frame?
		public bool descendingSlopeAutoCurr, descendingSlopeAutoPrev; //is object allowed to auto-descend slope this physics frame? Previous physics frame?
		public float autoDescentDirectionCurr, autoDescentDirectionPrev; //1, 0, or -1 based on direction slope is facing
		public float slopeAngleCurr, slopeAnglePrev; //angle of slope encountered this physics frame, and angle of slope in previous physics frame

		public void Reset() {
			//Update previous physics frame fields here
			onOneWayPrev = onOneWayCurr;
			slopeAnglePrev = slopeAngleCurr;
			descendingSlopeAutoPrev = descendingSlopeAutoCurr;
			autoDescentDirectionPrev = autoDescentDirectionCurr;

			//Update current physics frame fields here
			above = below = left = right = false;
			climbingSlope = descendingSlopeManual = descendingSlopeAutoCurr = false;
			onOneWayCurr = false;
			slopeAngleCurr = autoDescentDirectionCurr = 0;
		}
	}

	//Handles resetting the colliderName/colliderTag to "". Names reset either when object turns around OR when timer runs out
	void ColliderNamesReset() {
		if (Mathf.Sign(currMoveUnadjusted.x) != Mathf.Sign(prevMoveUnadjusted.x) && currMoveUnadjusted.x != 0.0f) {
			timeUntilResetCollider = -1f;
			collisions.colliderName = "";
			collisions.colliderTag = "";
		}

		if (!collisions.colliderName.Equals("") && timeUntilResetCollider == -0.1f) {
			timeUntilResetCollider = 0.1f;			
		}

		if (timeUntilResetCollider > 0.0f) {
			timeUntilResetCollider -= Time.deltaTime;
		} else {
			timeUntilResetCollider = -1f;
			collisions.colliderName = "";
			collisions.colliderTag = "";
		}
	}

	//GETTER & SETTERS:
	public bool Below() {
		return collisions.below;
	}

	public bool Above() {
		return collisions.above;
	}

	public bool Left() {
		return collisions.left;
	}

	public bool Right(){
		return collisions.right;
	}

	public bool OnOneWayPrev() {
		return collisions.onOneWayPrev;
	}

	public string ColliderName() {
		return collisions.colliderName;
	}

	public bool Falling() {
		return (!Below () && !Above () && (currMoveAdjusted.y < 0));
	}

	public void SetOnOneWay(bool value) {
		collisions.onOneWayCurr = value;
	}

	public void SetDropDown(bool value) {
		dropDown = value;
	}
}
