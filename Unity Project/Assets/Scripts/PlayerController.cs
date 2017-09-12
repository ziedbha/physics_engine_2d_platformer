using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* SCRIPT GUIDELINES:
 * This script is important as all Player controls and movement is decided here. Be careful what you put here.
 * Cluttering this script will only hurt the structure of our code later.
 * Limit additions to general movement (no core abilities/special abilities) and collision check!
 * 
 * TIP: Other scripts can reference this one. If you feel that another script might encapsulate functionalities better,
 * then create one and reference this one to access variables. Set private variables as internal if you need to access them
 * from other scripts.
 */

[RequireComponent (typeof(PhysicsController))]
[RequireComponent (typeof(AnimationController))]
public class PlayerController : MonoBehaviour {
	//Public fields
	[Header ("General Movement")]
	public float gravity = -80;
	public float moveSpeed = 12;
	public float climbSpeedFactor = 0.7f; //how much of the speed is climb speed
	public float fastFallFactor = 3; //how much more gravity is added to fastfalling

	[Header ("Velocity Modifiers")]
	[Range(0.0f, 1f)]
	public float xInputLimit = 0.5f; //separator between walk and run speeds
	[Range(0.0f, 1f)]
	public float yInputLimit = 0.5f; //separator in vertical movement
	public float minVelocityY = -40f; //minimum downwards velocity possible
	public float velocityCutoffX = 0.2f; //minimum required horizontal velocity required for Player to move (in absolute value terms)
	public float decelerationTimeAirborne = 0.1f; //deceleration in air (x-direction)
	public float decelerationTimeGround = 0.07f; //deceleration in ground (x-direction)
	public float decelerationTimeDashing = 0.5f; //deceleration when dashing (x-direction)

	//Private to Player
	bool disableGravity = false; //is gravity disabled this frame?
	bool falling = false; //is the player falling this frame?
	float velocityXSmoothing; //snappines of movement in x-direction

	//Accessible by other scripts
	internal bool inAir = true; //is the player in the air this frame?
	internal bool wallJumped = false; //did the player walljump this frame?
	internal bool jumped = false; //did the player jump this frame? (not wall jump)
	internal bool freezeSpriteDirection = false; //Can the player currently change the way it is facing
	internal Vector2 velocity = Vector2.zero; //velocity vector to be applied on the object's Move()

	//Input of player
	internal Vector2 directionalInput; //directional input of player (analog) this frame
	internal Vector2 directionalInputPrev = Vector2.zero; //directional input of player in previous frame
	internal Vector2 directionalInputRaw; //directional input of player (discrete) this frame
	internal Dictionary<string, bool> inputButtons = new Dictionary<string, bool>();
	internal Dictionary<string, float> inputReset = new Dictionary<string, float>();
	internal DoubleClicker doubleClickerDown;

	//Initialize in Awake()!
	internal PhysicsController controller; //collision and physics controller of Player
	internal AbilityTimeManager abilityManager;
	internal BasicAbilities basicAbilities; //manager for Basic Abilities of Player
	internal CoreAbilities coreAbilities; //manager for Basic Abilities of Player
	internal AnimationController anim;

	void Awake() {
		controller = GetComponent<PhysicsController> ();
		basicAbilities = GetComponent<BasicAbilities> ();
		coreAbilities = GetComponent<CoreAbilities> ();
		anim = GetComponent<AnimationController> ();

		abilityManager = new AbilityTimeManager ();
	}

	void Start () {
		List<KeyCode> downKeyList = new List<KeyCode>();
		downKeyList.Add (KeyCode.S);
		downKeyList.Add (KeyCode.DownArrow);
		doubleClickerDown = new DoubleClicker (downKeyList);

		controller.directionalInput = directionalInput;
		controller.climbSpeedFactor = climbSpeedFactor;

		inputButtons.Add ("Space", false);
		inputButtons.Add ("LShift", false);
		inputButtons.Add ("LControl", false);
		inputButtons.Add ("DoubleDown", false);

		inputReset.Add ("DoubleDown", 0.0f);

		anim.AddCondition ("isRunning", () => controller.Below () && directionalInput.x != 0 && !basicAbilities.isCrouching);
		anim.AddCondition ("isGrounded", () => controller.Below ());
		anim.AddCondition ("isFalling", () => inAir && velocity.y < 0);
		anim.AddCondition ("isJumping", () => (jumped || wallJumped) && !basicAbilities.isCrouching);
		anim.AddCondition ("isWallClinging", () => false);
		anim.AddCondition ("isDashing", () => coreAbilities.isDashing);
		anim.AddCondition ("isIdle", () => !(controller.Below () && directionalInput.x != 0) && controller.Below () 
			&& !inAir && !(wallJumped || jumped));
	}
		
	void Update () {
		DirectionalInputUpdate (); //get directional input, adjust it, and set the face direction in this frame
		ButtonInputUpdate ();
	}

	//Called each physics frame
	void FixedUpdate() {
		//IMPORTANT - Reset all 'did x this frame' variables BEFORE updating them, not at end
		jumped = false;
		wallJumped = false;
		abilityManager.Update (Time.deltaTime); //updates cooldowns
		CollisionCheck (); //updates state of player based on previous collisions

		float targetVelocityX = 0;
		//BASIC ABILITIES
		basicAbilities.JumpController ();
		basicAbilities.WallJumpController ();
		basicAbilities.DropDownController ();
		basicAbilities.CrouchController ();

		//CORE ABILITIES
		disableGravity = coreAbilities.DashController (ref targetVelocityX) || coreAbilities.GroundPoundController ();

		//RESETTING INPUTS
		inputButtons ["DoubleDown"] = false;
		inputReset ["DoubleDown"] = 0.0f;
		inputButtons ["Space"] = false;
		inputButtons ["LShift"] = false;
		inputButtons ["LControl"] = false;

		//MOVEMENT CALCULATIONS
		if (!basicAbilities.isCrouching && !coreAbilities.isDashing && !coreAbilities.isGroundPounding) {
			ComputeHorizontalMove (ref targetVelocityX); //get horizontal movement
		}
		VelocityUpdate (targetVelocityX, Time.deltaTime); //update velocity vector
		IgnoreCollisions(); //checks if player is allowed to ignore collisions this physics frame based on use of abilities
		//MOVING THE PLAYER
		controller.Move (velocity * Time.deltaTime); //move player		
	}

	void LateUpdate() {
		if (basicAbilities.isWallCling) {
			anim.SetFaceDirection (-basicAbilities.wallDirection);
		} 
	}

	//MAIN METHODS *********************************************************************************************************************************
	//**********************************************************************************************************************************************
	//Gets Directional Input, clamps it, and assigns the face direction. Must be called each frame
	void DirectionalInputUpdate() {
		directionalInputPrev = directionalInput;
		directionalInput = new Vector2 (Input.GetAxis ("Horizontal"), Input.GetAxis ("Vertical"));
		directionalInputRaw = new Vector2 (Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
		SetDirectionalInputInRange ();
		controller.directionalInput = directionalInput;
		FaceDirectionCheck ();
	}

	void ButtonInputUpdate() {
		//Check for using space button
		if (Input.GetKeyDown(KeyCode.Space)) {
			inputButtons ["Space"] = true;
		}

		//Check for double tapping down button
		if (doubleClickerDown.DoubleClickCheck()) {
			inputButtons ["DoubleDown"] = true;
			inputReset ["DoubleDown"] = 0.3f;
		}
		ResetInput ("DoubleDown", Time.deltaTime);

		if (Input.GetButtonDown ("Dash")) {
			inputButtons ["LShift"] = true;
		}

		if (Input.GetButton ("Crouch")) {
			inputButtons ["LControl"] = true;
		}
	}

	//Checking collisions and updating variables accordingly. Must be called each frame
	void CollisionCheck() {
		inAir = true;
		//checking if Player is colliding with an object above
		if (controller.Above() ) {
			velocity.y = 0;
		}
		//checking if Player is is colliding with an object below
		if (controller.Below()) {
			velocity.y = 0;
			inAir = false;
		}
	}

	//Final updates to velocity based on input of Player. Must be called each frame
	void VelocityUpdate (float targetVelocityX, float step) {
		//****** Updating Velocity X *********/
		float deceleration = DecelerationPicker ();
		if (basicAbilities.isCrouching) {
			targetVelocityX = targetVelocityX * basicAbilities.crouchSpeedPercent;
		}
		velocity.x = Mathf.SmoothDamp (velocity.x, targetVelocityX, ref velocityXSmoothing, deceleration);
		//if Player too slow horizontally, don't move
		if (Mathf.Abs (velocity.x) < velocityCutoffX) {
			velocity.x = 0;
		}

		//****** Updating Velocity Y *********/
		GravityCheck (step);
		//clamping vertical movement so that Player does not go down too much
		velocity.y = Mathf.Clamp (velocity.y, minVelocityY, int.MaxValue);
	}

	//Checks if player is allowed to ignore some collisions this physics frame
	void IgnoreCollisions() {
		if (coreAbilities.isGroundPounding) {
			controller.ignoreVertical = true;
		} else {
			controller.ignoreVertical = false;
		}

		if (coreAbilities.isDashing) {
			controller.ignoreHorizontal = true;
		} else {
			controller.ignoreHorizontal = false;
		}
	}

	/************************************************HELPERS **************************************************************/
	//Defines ranges for horizontal directional input (walking, running...)
	void SetDirectionalInputInRange () {
		Tools.SetValueInRange(directionalInput.x, xInputLimit);
		Tools.SetValueInRange(directionalInput.y, yInputLimit);
	}

	void ResetInput(string input, float step) {
		if (inputReset[input] > 0.0f) {
			inputReset [input] -= step;
		} else if (inputReset[input] <= 0.0f && inputButtons[input]) {
			inputButtons [input] = false;
			inputReset [input] = 0.0f;
		}
	}

	//Anything that affects horizontal momentum is calculated here. Since dashing and moving and other movement 
	//abilities conflict, we make it so they trigger in a set order, and don't do anything unless no 
	//abilities of higher priority have.
	void ComputeHorizontalMove(ref float targetVelocityX) {
		if (targetVelocityX == 0) {
			targetVelocityX = directionalInput.x * moveSpeed;
		}
	}

	//Determines the appropriate deceleration float, depending on location.
	float DecelerationPicker() {
		if (inAir && !coreAbilities.isDashing) {
			return decelerationTimeAirborne;
		} else if ((controller.Left() || controller.Right()) && controller.Below() && !coreAbilities.isDashing) {
			return 0.0f;
		} else if (coreAbilities.isDashing) {
			return decelerationTimeDashing;
		} else {
			return decelerationTimeGround;
		}
	}

	//Applies or removes gravity depending on the bool disabledGravity
	void GravityCheck(float step) {
		//only subject Player to gravity if it is enabled
		if (!disableGravity) {
			velocity.y += gravity * step;
		} else if (velocity.y < 0 && !coreAbilities.isGroundPounding) { //if gravity is disabled, then if velocity is downwards, then reset it to 0
			velocity.y = 0;
		}			
	}

	//Checks collisions and updates face direction accordingly
	void FaceDirectionCheck () {
		if (!freezeSpriteDirection) {
			if (controller.Below ()) {
				if (directionalInput.x != 0) {
					anim.SetFaceDirection (Mathf.Sign (directionalInput.x));
				}
			} else if (inAir) {
				if (velocity.x != 0) {
					anim.SetFaceDirection (Mathf.Sign (velocity.x));
				}
			}
		}		
	}

	//Returns true if Player is against a WallJumpable obstacle
	public bool AgainstWallJumpWall() {
		return (controller.Left() || controller.Right ()) && controller.collisions.colliderTag.Equals("WallJumpable");
	}
}
