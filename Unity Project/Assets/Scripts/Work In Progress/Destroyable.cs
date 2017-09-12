using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyable : RaycastController {
	public LayerMask playerCollisionMask; //objects it should collide with. Player only
	public CoreAbilities playerCoreAbilities; //reference to the Player's coreAbilities (checking for dashing etc...)

	GameObject player;
	string objtag;
	float rayLength = skinWidth + 0.01f;


	void Awake() {
		base.Awake ();
		objtag = gameObject.tag;
		player = GameObject.FindGameObjectWithTag ("Player");
	}

	void Start() {
		base.Start ();
	}

	void FixedUpdate() {
		UpdateRaycastOrigins ();
		if (Mathf.Abs (collider2d.transform.position.x - player.transform.position.x) < 30 && Mathf.Abs (collider2d.transform.position.y - player.transform.position.y) < 30) {
			switch (objtag) {
			case "DestroyOnGroundPound":
				RaycastGroundPound ();
				break;
			case "DestroyOnDash":
				RaycastDash ();
				break;
			}
		}

	}

	void RaycastGroundPound() {
		for (int i = 0; i < raycastVerticalCount; i++) { //similar raycast logic
			Vector2 rayOrigin = raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (verticalRaySpacing * i);
			RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.up, rayLength, playerCollisionMask);
			Debug.DrawRay (rayOrigin, Vector2.up, Color.green);
			if (hit) {
				if (playerCoreAbilities.isGroundPounding) {
					Destroy (gameObject);
				}
			}
		}
	}

	void RaycastDash() {
		for (int i = 0; i < raycastHorizontalCount; i++) { //similar raycast logic
			Vector2 rayOrigin = raycastOrigins.topLeft;
			rayOrigin += Vector2.down * (horizontalRaySpacing * i);
			RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.left, rayLength, playerCollisionMask);
			Debug.DrawRay (rayOrigin, Vector2.left, Color.green);
			if (hit) {
				if (playerCoreAbilities.isDashing) {
					Destroy (gameObject);
					return;
				}
			}
		}

		for (int i = 0; i < raycastHorizontalCount; i++) { //similar raycast logic
			Vector2 rayOrigin = raycastOrigins.topRight;
			rayOrigin += Vector2.down * (horizontalRaySpacing * i);
			RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right, rayLength, playerCollisionMask);
			Debug.DrawRay (rayOrigin, Vector2.right, Color.green);
			if (hit) {
				if (playerCoreAbilities.isDashing) {
					Destroy (gameObject);
					return;
				}
			}
		}	
	}
}
