using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(PlayerController))]
public class BasicAbilities : MonoBehaviour {
	//JUMPING
	[Header ("Jump")]
	public float jumpHeight = 7;

	float jumpVelocity;
	bool hasGroundJump = true;
	bool hasJump = true; 

	[Header ("WallJump")]
	public float wallJumpSpeed = 25;
	public float wallClingSpeedMax = 3; //speed at which Player slides off of the wall
	public float wallClingTimeout = 0.3f;
	public float wallJumpInputTimeout = 0.1f; 

	internal int wallDirection = 0; //direction the wall Player walljumped off of is facing. -1 = Left, 1 = Right
	internal bool isWallCling = false; //is the Player clinging on a wall?

	bool hasWallCling = false; 
	string prevWall = ""; //gameObject name of the previous wall Player walljumped off of
	string currWall = ""; //gameObject name of the current wall Player walljumped off of

	//DROPPING DOWN PLATFORMS
	[Header ("Platform Drop Down")]
	public float dropDownSpeed = 10;

	//CROUCHING
	[Header ("Crouch")]
	[Range(0.1f, 1f)]
	public float crouchHeightPercent = 0.5f;
	[Range(0.1f, 1f)]
	public float crouchSpeedPercent = 0.5f;

	internal bool isCrouching = false;
	float oldOffset;
	float shrinkSize;

	//Other references
	PlayerController player; //retrive on Awake()!

	void Awake() {
		player = GetComponent<PlayerController> ();
	}

	void Start() {
		jumpVelocity = Mathf.Sqrt(Mathf.Abs(player.gravity * jumpHeight)); //calculate jump speed based on gravity applied on player
		oldOffset = player.controller.collider2d.size.y * Mathf.Pow(crouchHeightPercent, 2);

		player.controller.SetDropDown(false);

		player.abilityManager.AddChildEvent ("GroundJump", "AnyJump");
		player.abilityManager.AddChildEvent ("DoubleJump", "AnyJump");
		player.abilityManager.AddChildEvent ("WallJump", "AnyJump");

		//Walljump input is locked out briefly
		player.abilityManager.AddConflict ("WallJumpInput", "WallJumpInput", wallJumpInputTimeout);
	}

	void FixedUpdate() {
		CollisionCheck ();
	}

	//Updates fields based on collision states of Player
	void CollisionCheck() {
		if (player.controller.Below()) {
			hasGroundJump = true;
			hasWallCling = true;
			prevWall = "";
			currWall = "";
		}		
	}

	//JUMPING ***********************************************************************************************************************
	// *******************************************************************************************************************************
	public void JumpController() {
		if (player.inputButtons["Space"] && !isWallCling && !player.coreAbilities.isDashing) { //if not clinging on wall and jump is allowed
			if (player.inAir) { //if in air, allow one jump only
				if (hasGroundJump && player.abilityManager.Trigger ("DoubleJump")) { //if ground jump is available, do it
					player.jumped = true;
					player.velocity.y = jumpVelocity;
					hasGroundJump = false;
				}
			} else if (player.abilityManager.Trigger ("hasGroundJump")){ //if on ground, do a normal jump
				player.jumped = true;
				player.velocity.y = jumpVelocity;
			}
		}
	}

	//WALLJUMPING ***********************************************************************************************************************
	// *******************************************************************************************************************************
	public void WallJumpController() {
		if (player.AgainstWallJumpWall() && player.inAir) {
			//if colliding with a walljumpable object on the left or right and not in air 
			if (hasWallCling) { //if Player is sliding down a wall
				wallDirection = player.controller.Left() ? 1 : -1;
				if (!isWallCling && !player.controller.ColliderName().Equals(prevWall)) {
					//if cling timer ran out and the wall is different than previous one
					hasWallCling = false;
					isWallCling = true;
					currWall = player.controller.ColliderName();
				}
			}
		} else {
			isWallCling = false;
		}

		if (isWallCling && player.inputButtons["Space"] && player.abilityManager.Trigger ("WallJump") 
			&& player.abilityManager.Trigger ("WallJumpInput")) { //wall jump happens here
			//Walljump fields update
			hasWallCling = true;
			isWallCling = false;
			prevWall = currWall;

			//Player fields update
			player.wallJumped = true;
			player.velocity.x = wallJumpSpeed * wallDirection;
			player.velocity.y = jumpVelocity;
		}
	
		if (player.abilityManager.IsLockedOut("WallJumpInput")) { //delays input on the opposite horizontal direction the wall is facing
			switch (wallDirection) {
			case -1:
				if (player.directionalInput.x > 0) {
					player.directionalInput.x = 0;
				}
				break;
			case 1:
				if (player.directionalInput.x < 0) {
					player.directionalInput.x = 0;
				}
				break;
			}
		}
	}

	//DROPPING DOWN ****************************************************************************************************************
	// *************************************************************************************************************************************
	//handles the dropping down mechanics
	public void DropDownController() {
		player.controller.SetDropDown (false);
		if (player.inputButtons["DoubleDown"]) {
			if (player.controller.OnOneWayPrev()) { //to outspeed moving most moving platforms and give a good drop down feel
				player.controller.SetDropDown(true);	
				player.controller.SetOnOneWay(false);
				player.velocity.y -= dropDownSpeed;
			}
		}
	}

	//CROUCHING ****************************************************************************************************************
	// *************************************************************************************************************************************
	public void CrouchController() {
		//press right ctrl on ground, initiate crouch: collider height, speed modifier
		//not on ground = not crouching. can dash still for now
		if (player.inputButtons["LControl"] && player.controller.Below() && !isCrouching) {
			float currentSizeX = player.controller.collider2d.size.x;
			float currentSizeY = player.controller.collider2d.size.y;
			float currentOffsetX = player.controller.collider2d.offset.x;
			float currentOffsetY = player.controller.collider2d.offset.y;
			shrinkSize = currentSizeY * crouchHeightPercent;
			player.controller.collider2d.size = new Vector2 (currentSizeX, currentSizeY * crouchHeightPercent);

			player.controller.collider2d.offset = new Vector2(currentOffsetX, currentOffsetY - oldOffset);
			player.controller.CalculateRaySpacing ();
			isCrouching = true;
			player.inputButtons ["LControl"] = false;
		} else if (isCrouching && (!player.inputButtons["LControl"] || !player.controller.Below()) && !CollideAbove(shrinkSize)) {
			float currentSizeX = player.controller.collider2d.size.x;
			float currentSizeY = player.controller.collider2d.size.y;
			float currentOffsetX = player.controller.collider2d.offset.x;
			float currentOffsetY = player.controller.collider2d.offset.y;
			player.controller.collider2d.size = new Vector2 (currentSizeX, currentSizeY / crouchHeightPercent);
			player.controller.collider2d.offset = new Vector2(currentOffsetX, currentOffsetY + oldOffset);
			player.controller.CalculateRaySpacing ();

			isCrouching = false;
		}

		if (isCrouching) {
			player.velocity.x = 0;
			player.velocity.y = 0;
		}
	}

	bool CollideAbove(float rayLength) {
		for (int i = 0; i < player.controller.raycastVerticalCount; i++) {
			Vector2 rayOrigin = player.controller.raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (player.controller.verticalRaySpacing * i);
			RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.up, rayLength, player.controller.obstacleCollisionMask);
			Debug.DrawRay (rayOrigin, Vector2.up, Color.blue);
			if (hit) {
				return true;
			}
		}
		return false;
	}
}
