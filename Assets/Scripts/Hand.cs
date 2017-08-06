using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour {

    private const float MOVE_DELAY = 0.125f;
    public List<Magnus> cards;
    private Magnus currentMagnus;
    private int currentPosition;
    private float lastTimeMoved;

	// Use this for initialization
	void Start() {
		
	}
	
	// Update is called once per frame
	void Update() {
        float inputX = Input.GetAxisRaw("Horizontal");
        // Allow us to move the cursor freely if we stop holding the button down
        if (inputX == 0) {
            lastTimeMoved = 0;
        }
        if (Time.time - lastTimeMoved >= MOVE_DELAY) {
            if (inputX > 0 && currentPosition < cards.Count - 1) {
                moveToPosition(currentPosition + 1);
            }
            if (inputX < 0 && currentPosition > 0) {
                moveToPosition(currentPosition - 1);
            }
        }
	}

    void moveToPosition(int position) {
        Transform cardGraphic = null;
        if (currentMagnus != null) {
            cardGraphic = currentMagnus.transform.Find("Graphic");
            cardGraphic.GetComponent<SpriteRenderer>().color = Color.white;
        }
        currentPosition = position;
        currentMagnus = cards[currentPosition];
        cardGraphic = currentMagnus.transform.Find("Graphic");
        cardGraphic.GetComponent<SpriteRenderer>().color = Color.yellow;
        transform.position = currentMagnus.transform.Find("Hand Position").position;
        lastTimeMoved = Time.time;
    }
}
