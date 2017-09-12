using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraFollow : MonoBehaviour {

	public PhysicsController target;
	public float verticalOffset = 0.5f; //distance above player, where the camera looks at by default
	public float lookVerticalDist = 8; //distance used to look down or up when holding up or down
	public float lookHorizontalDist  = 4; //distance used when looking ahead
	public float lookHoldTime = 1; //# frames needed to look up or down when holding direction
	public float lookFallingTime = 0.1f; //# frames needed for the camera to speed up on a falling down state
	public float horizontalSmoothTime = 0.5f; //higher means slower catchup
	public float verticalSmoothTime = 0.2f; //higher means slower catchup
	public Vector2 focusAreaSize = new Vector2(3, 5);

	float currentLookAheadX;
	float targetLookAheadX;
	float lookAheadDirX;
	float smoothLookVelocityX;
	float smoothVelocityY;
	float directionalY;
	bool lookAheadStopped;

	bool isCentered = true;
	bool scrolledLeft = false;
	bool scrolledRight = false;

	float timeUntilLookUp = -1f;
	bool lookUpRoutine = false;
	bool hasLookUp = false;

	float timeUntilLookDown = 0;
	bool lookDownRoutine = false;
	bool hasLookDown = false;

	float timeUntilFalling = 0;
	bool fallingRoutine = false;
	bool hasFalling = false;

	FocusArea focusArea;
	Dictionary<string, bool> inputButtons = new Dictionary<string, bool>();

	void Start() {
		focusArea = new FocusArea (target.collider2d.bounds, focusAreaSize);
		inputButtons.Add ("RControl", false);
	}

	//Called each frame
	void Update () {
		//Check for input on Right control
		if (Input.GetKeyDown(KeyCode.RightControl)) {
			inputButtons ["RControl"] = true;
		}
	}

	void FixedUpdate() {
		focusArea.Update (target.GetComponent<Collider2D>().bounds);
		Vector2 focusPosition = focusArea.center + Vector2.up * verticalOffset;
		directionalY = target.directionalInput.y;

		//TIMER MANAGEMENT FOR LOOKING UP/DOWN
		//Looking up
		LookUpCheck ();
		//Looking down
		LookDownCheck ();
	
		//Falling
		FallingCheck ();

		//VERTICAL ADJUSTMENT DUE TO LOOKING UP/DOWN
		if (hasLookDown) {
			focusPosition = focusArea.center + Vector2.down * lookVerticalDist;
		}
		if (hasLookUp) {
			focusPosition = focusArea.center + Vector2.up * lookVerticalDist;
		}
			
		//HORIZONTAL ADJUSTMENT DUE TO SCROLLING
		if (scrolledLeft && target.directionalInput.x > 0) {
			targetLookAheadX = 0;
			scrolledLeft = false;
		} 

		if (scrolledRight && target.directionalInput.x < 0) {
			targetLookAheadX = 0;
			scrolledRight = false;
		}

		if (focusArea.velocity.x != 0 && isCentered) {
			lookAheadDirX = Mathf.Sign (focusArea.velocity.x);
			if (Mathf.Sign(target.directionalInput.x) == Mathf.Sign(focusArea.velocity.x) && target.directionalInput.x != 0) {
				lookAheadStopped = false;
				targetLookAheadX = lookAheadDirX * lookHorizontalDist;
			} else {
				if (!lookAheadStopped) {
					lookAheadStopped = true;
					targetLookAheadX = currentLookAheadX + (lookAheadDirX * lookHorizontalDist - currentLookAheadX)/4f;
				}
			}
		}

		ScrollingCheck();

		//Moving the camera positions
		currentLookAheadX = Mathf.SmoothDamp (currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, horizontalSmoothTime);

		focusPosition.x += currentLookAheadX;
		focusPosition.y = Mathf.SmoothDamp (transform.position.y, focusPosition.y, ref smoothVelocityY, 
			(hasFalling) ? verticalSmoothTime/3 : verticalSmoothTime);

		transform.position = (Vector3) focusPosition + Vector3.forward * -10;
	}

	//LOOKING UP ADJUSTING METHODS
	void LookUpCheck() {
		if (directionalY != 1) {
			hasLookUp = false;
		} else if (!lookUpRoutine) {
			lookUpRoutine = true;
			timeUntilLookUp = Time.time + lookHoldTime;
		}

		if (lookUpRoutine) {
			LookUpTimer (Time.deltaTime);
		}
	}
		
	void LookUpTimer(float step) {
		if (Time.time < timeUntilLookUp) {
			if (directionalY != 1 || !target.Below()) {
				hasLookUp = false;
				lookUpRoutine = false;
				timeUntilLookUp = -1f;
			}
		} else {
			lookUpRoutine = false;
			hasLookUp = true;
			timeUntilLookUp = -1f;
		}
	}

	//LOOKING DOWN ADJUSTING METHODS
	void LookDownCheck() {
		if (directionalY != -1) {
			hasLookDown = false;
		} else if (!lookDownRoutine) {
			lookDownRoutine = true;
			timeUntilLookDown = Time.time + lookHoldTime;
		}

		if (lookDownRoutine) {
			LookDownTimer (Time.deltaTime);
		}
	}

	void LookDownTimer(float step) {
		if (Time.time < timeUntilLookDown) {
			if (directionalY != -1 || !target.Below()) {
				hasLookDown = false;
				lookDownRoutine = false;
				timeUntilLookUp = -1f;
			}
		} else {
			lookDownRoutine = false;
			hasLookDown = true;
			timeUntilLookDown = -1f;
		}
	}

	//FALLING ADJUSTING METHODS
	void FallingCheck() {
		if (!target.Falling()) {
			hasFalling = false;
		} else if (!fallingRoutine) {
			fallingRoutine = true;
			timeUntilFalling= Time.time + lookFallingTime;
		}

		if (fallingRoutine) {
			FallingTimer (Time.deltaTime);
		}	
	}

	void FallingTimer(float step) {
		if (Time.time < timeUntilFalling) {
			if (!target.Falling()) {
				hasFalling = false;
				fallingRoutine= false;
				timeUntilFalling = -1f;
			}
		} else {
			fallingRoutine = false;
			hasFalling = true;
			timeUntilLookDown = -1f;
		}
	}

	//SCROLLING CHECK METHODS
	void ScrollingCheck() {
		if (target.directionalInput.x < 0 && inputButtons["RControl"]) {
			if (!scrolledLeft) {
				isCentered = false;
				scrolledLeft = true;
				scrolledRight= false;
				targetLookAheadX = lookHorizontalDist * -3;
			} else {
				isCentered = true;
				scrolledLeft = false;
				targetLookAheadX = 0;	
			}
			inputButtons ["RControl"] = false;
		}

		if (target.directionalInput.x > 0 && inputButtons["RControl"]) {
			if (!scrolledRight) {
				isCentered = false;
				scrolledRight= true;
				scrolledLeft = false;
				targetLookAheadX = lookHorizontalDist * 3;	
			} else {
				isCentered = true;
				scrolledRight = false;
				targetLookAheadX = 0;					
			}
			inputButtons ["RControl"] = false;
		} 

		if (inputButtons["RControl"]) {
			if (scrolledLeft) {
				isCentered = true;
				scrolledLeft = false;
				targetLookAheadX = 0;				
			}

			if (scrolledRight) {
				isCentered = true;
				scrolledRight = false;
				targetLookAheadX = 0;				
			}
			inputButtons ["RControl"] = false;
		}
	}

	//FOCUS AREA INFO
	struct FocusArea {
		public Vector2 center;
		public Vector2 velocity;
		float left, right, top, bottom;

		public FocusArea(Bounds targetBounds, Vector2 size) {
			left = targetBounds.center.x - size.x/2;
			right = targetBounds.center.x + size.x/2;
			bottom = targetBounds.min.y;
			top = targetBounds.min.y + size.y;

			velocity = Vector2.zero;
			center = new Vector2((left + right)/2,(top + bottom)/2);
		}

		public void Update(Bounds targetBounds) {
			float shiftX = 0;
			if (targetBounds.min.x < left) {
				shiftX = targetBounds.min.x - left;
			} else if (targetBounds.max.x > right) {
				shiftX = targetBounds.max.x - right;
			}
			left += shiftX;
			right += shiftX;

			float shiftY = 0;
			if (targetBounds.min.y < bottom) {
				shiftY = targetBounds.min.y - bottom;
			} else if (targetBounds.max.y > top) {
				shiftY = targetBounds.max.y - top;
			}
			top += shiftY;
			bottom += shiftY;
			center = new Vector2((left + right)/2,(top + bottom)/2);
			velocity = new Vector2 (shiftX, shiftY);
		}
	}

	//VISUAL TOOLS
	void OnDrawGizmos() {
		Gizmos.color = new Color (1, 0, 0, .5f);
		Gizmos.DrawCube (focusArea.center, focusAreaSize);
	}
}