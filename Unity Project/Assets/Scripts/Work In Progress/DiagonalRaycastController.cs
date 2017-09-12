using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(BoxCollider2D))]
public class DiagonalRaycastController : MonoBehaviour {
	BoxCollider2D collider2d;

	void Awake() {
		collider2d = GetComponent<BoxCollider2D> ();
	}

	void FixedUpdate() {
		GetBoxCorners ();
	}

	public void GetBoxCorners() {
		Vector2 size = collider2d.size;
		//Vector2 centerPoint = new Vector2(collider2d.offset.x, collider2d.offset.y);
		Vector3 worldPos = transform.TransformPoint (collider2d.offset);

		float top = (size.y * Mathf.Abs(transform.localScale.y) / 2f);
		float bottom = - (size.y * Mathf.Abs(transform.localScale.y) / 2f);
		float left = - (size.x * Mathf.Abs(transform.localScale.x) / 2f);
		float right = (size.x * Mathf.Abs(transform.localScale.x) /2f);

		Vector3 topLeft = new Vector2 (left, top);
		Vector3 topRight = new Vector2 (right, top);
		Vector3 bottomLeft = new Vector2 (left, bottom);
		Vector3 bottomRight = new Vector2 (right, bottom);

		Vector3 topMiddle = new Vector2 (left + 1, top);

		topLeft = RotatePointAroundPivot(topLeft, Vector3.zero, collider2d.transform.eulerAngles);
		topRight = RotatePointAroundPivot(topRight, Vector3.zero, collider2d.transform.eulerAngles);
		bottomLeft = RotatePointAroundPivot(bottomLeft, Vector3.zero, collider2d.transform.eulerAngles);
		bottomRight = RotatePointAroundPivot(bottomRight, Vector3.zero, collider2d.transform.eulerAngles);
		topMiddle = RotatePointAroundPivot(topMiddle, Vector3.zero, collider2d.transform.eulerAngles);

		topLeft = topLeft + worldPos;
		topRight = topRight + worldPos;
		bottomLeft = bottomLeft + worldPos;
		bottomRight = bottomRight + worldPos;
		topMiddle = topMiddle + worldPos;

		Debug.DrawLine (topLeft, topLeft + Vector3.up * 10, Color.red);
		Debug.DrawLine (topRight, topRight + Vector3.up * 10, Color.blue);
		Debug.DrawLine (bottomLeft, bottomLeft + Vector3.down * 10, Color.yellow);
		Debug.DrawLine (bottomRight, bottomRight + Vector3.down * 10, Color.green);
		Debug.DrawLine (topMiddle, topMiddle + Vector3.up * 10, Color.magenta);
	}

	Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
		Vector3 dir = point - pivot; // get point direction relative to pivot
		dir = Quaternion.Euler(angles) * dir; // rotate it
		point = dir + pivot; // calculate rotated point
		return point; // return it
	}
}
