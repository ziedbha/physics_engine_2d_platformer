using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(PlayerController))]
public class CoreAbilities : MonoBehaviour {
	//DASHING
	[Header ("Dash")]
	public float dashFactor = 90; //how much to multiply speed by if dashing
	public float dashTimeout = 0.7f; //how many frames until the Player can dash again
	public float dashHoverTime = 0.3f; 

	internal bool isDashing = false; //is the player currently in dash state (encompasses all substates)?
	float dashHoverTimeoutDuration = 0;
	bool dashHover = false; //is the Player currently hovering due to a dash?
	bool hasAirDash = true; //does the player have the airdash?

	//GROUND POUNDING
	[Header ("Ground Pound")]
	public float groundPoundSpeed = 50; //how fast should the player go down
	public float groundPoundStartTime = 0.20f; //amount of time for the startup state of the ground pound

	internal bool isGroundPounding = false; //is the player currently in ground pound state (encompasses all substates)?
	bool groundPoundStart = false; //is the player currently in the startup state of ground pound?
	bool groundPound = false; //is player currently going downwards during ground pound?
	float timeUntilGroundPound = -1f; //internal clock time until player starts the downwards fall

	//Other references
	PlayerController player; //retrive on Awake()!

	void Awake() {
		player = GetComponent<PlayerController> ();
	}
		
	void Start () {
		//Dash locks itself out briefly
		player.abilityManager.AddConflict ("Dash", "Dash", dashTimeout);
		//Can't dash right after a jump. Fixes a 'long jump' bug.
		player.abilityManager.AddConflict ("Dash", "AnyJump", 0.1f);
	}

	void FixedUpdate() {
		CollisionCheck ();
	}

	//Updates fields based on collision states of Player
	void CollisionCheck() {
		if (player.controller.Below()) {
			hasAirDash = true;
		}		
	}
	
	//DASHING ****************************************************************************************************************
	// ******************************************************************************************************************************
	//handles dashing. Sets velocity based on input. Returns a bool (true if dashHover is on, false otherwise)
	public bool DashController(ref float targetVelocityX) {
		bool dash = false;
		if (!player.basicAbilities.isWallCling && !isGroundPounding) {
			if (player.inputButtons["LShift"]) {
				if (!player.inAir) {
					dash = true;
				} else if (hasAirDash) {
					dash = true;
				}
			}
		}

		//only modifies velocity if no ability of higher priority has previously modified it
		if (targetVelocityX == 0) {
			if (dash && player.abilityManager.Trigger("Dash")) {
				if (player.inAir) {
					player.velocity.y = 0;
					hasAirDash = false;
				}
				dashHoverTimeoutDuration = Time.time + dashHoverTime;
				isDashing = true;
				if (player.directionalInput.x == 0) {
					targetVelocityX = player.anim.FaceDirection() * player.moveSpeed * dashFactor;
				} else {
					targetVelocityX = ((Mathf.Sign (player.directionalInput.x) == -1) ? -1 : 1) * player.moveSpeed * dashFactor;
				}
			}
		}

		if (isDashing) {
			if (Time.time <= dashHoverTimeoutDuration) {
				if (!player.controller.Below()) { //only hover if in the air
					dashHover = true;
				} else {
					dashHover = false;
				}
			} else if (Time.time > dashHoverTimeoutDuration){
				dashHoverTimeoutDuration = -1f;
				isDashing = false;
				dashHover = false;
				player.velocity.x = 0;
			}	
		}
		return dashHover;
	}

	//GROUND POUNDING ****************************************************************************************************************
	// ******************************************************************************************************************************

	public bool GroundPoundController() {
		bool disableGravity = false;
		//press button + inAir: stay in air for a while
		if (player.inAir && player.inputButtons["DoubleDown"] && !isGroundPounding && !isDashing) {
			isGroundPounding = true; //start groundpounding sequence
			groundPoundStart = true; //ground pound startup
			timeUntilGroundPound = Time.time + groundPoundStartTime;
		}

		//after timer runs out, drop down really quick
		if (groundPoundStart) {
			if (Time.time >= timeUntilGroundPound) {
				groundPoundStart = false; //no more startup
				groundPound = true; //start actual downwards motion
				timeUntilGroundPound = -1f;
			}
			player.velocity.x = 0;
			player.velocity.y = 0;
			disableGravity = true;
		}

		//actual downwards motion
		if (groundPound) { 
			player.velocity.y = -groundPoundSpeed;
			player.velocity.x = 0;
			disableGravity = true;
		}

		//start moving
		if (groundPound && player.controller.Below()) {
			groundPound = false;
			isGroundPounding = false;

			player.velocity.x = 0;
			player.velocity.y = 0;
			disableGravity = false;
		}
		return disableGravity;
	}
}
