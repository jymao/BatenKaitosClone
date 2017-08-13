using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hand : MonoBehaviour {

    private const float MOVE_DELAY = 0.125f;
    private float lastTimeMoved;

    private bool canSelect = false;
    private bool canMove = true;
    private bool turnEnded = false; //prevents card from being selected when trying to close results screen

    private List<GameObject> cards = new List<GameObject>();
    private GameObject selectedMagnus;
    private int currentPosition = 0;
    private int numCardsPlayed = 0;

    public GameManager gameManager;
    public Transform bottomBar;
    public Transform magnusHandSpace;
    public Transform nextMagnusSpace;
    public Transform currMagnusSpace;

    public void SetTurnEnded(bool b) { turnEnded = b; }
    public void ResetNumCardsPlayed() { numCardsPlayed = 0; }
    public void SetCanSelect(bool b) { 
        canSelect = b;
        //Cursor becomes an hourglass if canSelect is false
        if (canSelect) {
            GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Images/cursor");
        }
        else {
            GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Images/hourglass");
        }
    }

    public void initialize() {
        //draw new hand from deck
        NewHand();

        selectedMagnus = cards[currentPosition];
        if (selectedMagnus != null) {
            Transform cardGraphic = selectedMagnus.transform.GetChild(0);
            cardGraphic.GetComponent<SpriteRenderer>().color = Color.yellow;
        }

        //SetCanSelect(true);
	}
	
	// Update is called once per frame
	void Update() {
        float inputX = Input.GetAxisRaw("Horizontal");
        // Allow us to move the cursor freely if we stop holding the button down
        if (inputX == 0) {
            lastTimeMoved = 0;
        }

        if (canMove && Time.time - lastTimeMoved >= MOVE_DELAY) {
            if (inputX > 0 && currentPosition < cards.Count - 1) {
                moveToPosition(currentPosition + 1);
            }
            if (inputX < 0 && currentPosition > 0) {
                moveToPosition(currentPosition - 1);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && canSelect && numCardsPlayed < 9 && cards.Count != 0) {
            if (!turnEnded) {
                ChooseCard(0);
            }
            else {
                turnEnded = false;
            }
        }

        if (canSelect && numCardsPlayed < 9 && cards.Count != 0) {
            if (!turnEnded) {
                
                int spiritNumberCount = selectedMagnus.GetComponent<Magnus>().GetSpiritNumberCount();
                if (Input.GetKeyDown(KeyCode.Alpha1)) {
                    ChooseCard(0);
                } else if (Input.GetKeyDown(KeyCode.Alpha2) && spiritNumberCount >= 2) {
                    ChooseCard(1);
                } else if (Input.GetKeyDown(KeyCode.Alpha3) && spiritNumberCount >= 3) {
                    ChooseCard(2);
                } else if (Input.GetKeyDown(KeyCode.Alpha4) && spiritNumberCount == 4) {
                    ChooseCard(3);
                }
            } else {
                turnEnded = false;
            }
        }
	}

    private void moveToPosition(int position) {
        Transform cardGraphic = null;
        if (selectedMagnus != null) {
            cardGraphic = selectedMagnus.transform.GetChild(0);
            cardGraphic.GetComponent<SpriteRenderer>().color = Color.white;
        }
        currentPosition = position;
        selectedMagnus = cards[currentPosition];
        SetDescription(selectedMagnus);
        cardGraphic = selectedMagnus.transform.GetChild(0);
        cardGraphic.GetComponent<SpriteRenderer>().color = Color.yellow;
        transform.position = new Vector3(selectedMagnus.transform.position.x, transform.position.y, transform.position.z);
        lastTimeMoved = Time.time;
    }

    public void NewHand() {
        DrawCard();
        for (int i = 0; i < 6; i++) {
            ShiftCards(0, i);
            DrawCard();
        }

        //restore previous card's sprite color and get proper position in new hand
        moveToPosition(currentPosition);
    }

    public void DiscardHand() {
        while (cards.Count > 0) {
            gameManager.Discard(cards[0]);
            cards.RemoveAt(0);
        }
    }

    private bool DrawCard() {
        GameObject card = gameManager.DrawCard();

        if (card != null) {
            cards.Add(card);
            card.transform.position = magnusHandSpace.position;
            return true;
        }

        return false;
    }

    private void ShiftCards(int start, int end) {
        for (int i = start; i <= end; i++) {
            Magnus magnusScript = cards[i].GetComponent<Magnus>();
            cards[i].transform.position = cards[i].transform.position - new Vector3(magnusScript.GetWidth(), 0, 0);
        }
    }

    private void ChooseCard(int spiritNumberIndex) {
        if (!gameManager.GetFirstMoveDone()) {
            gameManager.SetFirstMoveDone(true);
            gameManager.SetTimeProcessed();
        }
        numCardsPlayed++;

        Magnus magnus = selectedMagnus.GetComponent<Magnus>();
        magnus.ChooseNumber(spiritNumberIndex);
        gameManager.playMagnus(new PlayedMagnus(magnus, magnus.GetSpiritNumber(spiritNumberIndex)));

        if (currMagnusSpace.childCount == 0) {
            selectedMagnus.transform.position = currMagnusSpace.position;
            selectedMagnus.transform.SetParent(currMagnusSpace, true);
            selectedMagnus.transform.localScale = new Vector3(1,1,1);
        }
        else {
            selectedMagnus.transform.position = nextMagnusSpace.position;
            selectedMagnus.transform.SetParent(nextMagnusSpace, true);
            selectedMagnus.transform.localScale = new Vector3(1, 1, 1);

            SetCanSelect(false);
        }
        cards.RemoveAt(currentPosition);

        //selected card not rightmost card in hand, shift cards on right normally and cursor position stays the same
        if (currentPosition != cards.Count) {
            ShiftCards(currentPosition, cards.Count - 1);
            moveToPosition(currentPosition);
        }

        bool cardDrawn = DrawCard();

        //cursor was rightmost, select newly drawn card
        if (cardDrawn && currentPosition == cards.Count - 1) {
            moveToPosition(currentPosition);
        }

        //cursor was rightmost and no new card drawn, move cursor left
        if (!cardDrawn && currentPosition == cards.Count) {
            //no more cards in hand
            if (currentPosition == 0) {
                //Revert chosen card's color
                if (selectedMagnus != null) {
                    Transform cardGraphic = selectedMagnus.transform.GetChild(0);
                    cardGraphic.GetComponent<SpriteRenderer>().color = Color.white;
                }
                SetDescription(null);
                //Hide cursor maybe?
            }
            else {
                moveToPosition(currentPosition - 1);
            }
        }

    }

    private void SetDescription(GameObject card) {
        if (card != null) {
            Magnus cardScript = card.GetComponent<Magnus>();
            string magnusName = cardScript.GetName().Replace("_", " ");
            string elementStat = cardScript.GetElementStat().ToString();
            string totalStat = (cardScript.GetNeutralStat() + cardScript.GetElementStat()).ToString();
            string element = cardScript.GetElement().ToString();

            string effect = "ATK/DEF ";
            if (cardScript.GetIsHeal()) {
                effect = "Heals " + totalStat + " HP";
            }
            else {
                if (element == "Neutral") {
                    effect += totalStat;
                }
                else {
                    effect += totalStat + " (" + element + " " + elementStat + ")";
                }
            }

            bottomBar.GetChild(1).GetComponent<Text>().text = magnusName;
            bottomBar.GetChild(2).GetComponent<Text>().text = effect;
        }
        else {
            bottomBar.GetChild(1).GetComponent<Text>().text = "";
            bottomBar.GetChild(2).GetComponent<Text>().text = "";
        }
    }

    public void Hide() {
        GetComponent<SpriteRenderer>().enabled = false;
        bottomBar.gameObject.SetActive(false);
        canMove = false;
        for (int i = 0; i < cards.Count; i++) {
            cards[i].GetComponent<Magnus>().Hide();
        }
    }

    public void Show() {
        GetComponent<SpriteRenderer>().enabled = true;
        bottomBar.gameObject.SetActive(true);
        canMove = true;
        for (int i = 0; i < cards.Count; i++) {
            cards[i].GetComponent<Magnus>().Show();
        }
    }
}
