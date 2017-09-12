using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelReloader : MonoBehaviour {

	void OnTriggerEnter2D (Collider2D other) {
		if (other.gameObject.tag == "Player") {
			int y = SceneManager.GetActiveScene ().buildIndex;
			SceneManager.LoadScene (y);
		}
	}
}
