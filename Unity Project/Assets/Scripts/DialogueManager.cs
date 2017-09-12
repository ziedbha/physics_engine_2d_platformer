using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour {
	public GameObject textBox;
	public Text currentText;
	public TextAsset file;
	public string[] lines;

	bool isTyping = false;
	int currentLine = 0;

	void Start () {
		currentText.text = "";
		if (file != null) {
			lines = file.text.Split ('\n');
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown ("f") && currentLine < lines.Length) {
			print ("test");
			if (isTyping) {
				StopCoroutine ("TypeText");
				isTyping = false;
				currentText.text = lines [currentLine];
				currentLine += 1;
			} else {
				currentText.text = "";
				StartCoroutine ("TypeText");
			}
		} else if (Input.GetKeyDown ("f") && currentLine >= lines.Length) {
			DisableDialogue ();
		}
	}

	IEnumerator TypeText () {
		isTyping = true;
		for (int i = 0; i < lines[currentLine].Length; i++) {
			currentText.text = currentText.text + lines [currentLine] [i];
			yield return null;
		}
		isTyping = false;
		currentLine += 1;
	}

	public void SetDialogue (TextAsset txt){
		file = txt;
	}

	public void EnableDialogue () {
		lines = file.text.Split ('\n');
		isTyping = false;
		currentLine = 0;
		textBox.SetActive (true);
	}

	void DisableDialogue () {
		textBox.SetActive (false);
	}
}
