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
    public Transform bottomBar; //Contains description for currently selected Magnus
    public Transform magnusHandSpace; //Newly drawn cards are moved here
    public Transform nextMagnusSpace; //Chosen cards are moved here if currMagnusSpace is occupied (basically queued up)
    public Transform currMagnusSpace; //First chosen card is moved here and nextMagnusSpace cards are moved here as cards are processed

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

    public void Initialize() {
        //draw new hand from deck
        NewHand();
	}
	
	// Update is called once per frame
	void Update() {
        float inputX = Input.GetAxisRaw("Horizontal");
        // Allow us to move the cursor freely if we stop holding the button down
        if (inputX == 0) {
            lastTimeMoved = 0;
        }

        //Moving hand cursor
        if (canMove && Time.time - lastTimeMoved >= MOVE_DELAY) {
            if (inputX > 0 && currentPosition < cards.Count - 1) {
                MoveToPosition(currentPosition + 1);
            }
            if (inputX < 0 && currentPosition > 0) {
                MoveToPosition(currentPosition - 1);
            }
        }

        //Selecting magnus. Multiple ways to select for each spirit number on the card.
        if (canSelect && cards.Count != 0) {
            if ((gameManager.GetIsPlayerTurn() && numCardsPlayed < 9) || (!gameManager.GetIsPlayerTurn() && gameManager.GetEnemyNumAttacks() > 0)) {
                if (!turnEnded) {
                    int spiritNumberCount = selectedMagnus.GetComponent<Magnus>().GetSpiritNumberCount();
                    if (Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.Space)) {
                        ChooseCard(0);
                    } else if (Input.GetKeyDown(KeyCode.J) && spiritNumberCount >= 2) {
                        ChooseCard(1);
                    } else if (Input.GetKeyDown(KeyCode.I) && spiritNumberCount >= 3) {
                        ChooseCard(2);
                    } else if (Input.GetKeyDown(KeyCode.K) && spiritNumberCount == 4) {
                        ChooseCard(3);
                    }
                }
                else {
                    //Input was used close the battle results screen.
                    //Prevents a card from being selected instantly when the turn starts.
                    if (Input.GetKeyDown(KeyCode.Space)) {
                        turnEnded = false;
                    }
                }
            }
        }
	}

    private void MoveToPosition(int position) {
        currentPosition = position;
        selectedMagnus = cards[currentPosition];
        SetDescription(selectedMagnus);
        
        transform.position = new Vector3(selectedMagnus.transform.position.x, transform.position.y, transform.position.z);
        lastTimeMoved = Time.time;
    }

    public void NewHand() {
        DrawCard();
        for (int i = 0; i < 6; i++) {
            ShiftCards(0, i);
            DrawCard();
        }

        //get proper position in new hand
        MoveToPosition(currentPosition);
    }

    public void DiscardHand() {
        while (cards.Count > 0) {
            gameManager.Discard(cards[0]);
            cards.RemoveAt(0);
        }
    }

    //Set valid status for each card in hand.
    //A Magnus's valid status can change depending on the turn and what cards have been played.
    public void ValidateHand() {
        for (int i = 0; i < cards.Count; i++) {
            gameManager.ValidateCard(cards[i]);
        }
    }

    //Draws a new card from the deck to the hand. Returns false is deck was empty and no card was drawn.
    private bool DrawCard() {
        GameObject card = gameManager.DrawCard();

        if (card != null) {
            cards.Add(card);
            card.transform.position = magnusHandSpace.position;
            return true;
        }

        return false;
    }

    //Shifts cards in hand over to make space for newly drawn card.
    private void ShiftCards(int start, int end) {
        for (int i = start; i <= end; i++) {
            Magnus magnusScript = cards[i].GetComponent<Magnus>();
            cards[i].transform.position = cards[i].transform.position - new Vector3(magnusScript.GetWidth(), 0, 0);
        }
    }

    //Choose a card and its spirit number
    private void ChooseCard(int spiritNumberIndex) {
        //First card played cancels the timer for the player to make a move.
        if (!gameManager.GetFirstMoveDone()) {
            gameManager.SetFirstMoveDone(true);
            gameManager.SetTimeProcessed();
        }
        numCardsPlayed++;

        Magnus magnus = selectedMagnus.GetComponent<Magnus>();
        magnus.ChooseNumber(spiritNumberIndex);
        gameManager.PlayMagnus(new PlayedMagnus(magnus, magnus.GetSpiritNumber(spiritNumberIndex)));
        ValidateHand();

        //Choosing a finisher special or an invalid Magnus ends the attack combo
        if (gameManager.GetIsPlayerTurn()) {
            if (!magnus.GetIsValid() || magnus.GetIsFinisher()) {
                SetCanSelect(false);
            }
        }

        //Move the Magnus to the appropriate space
        Transform playerCurrMagnus = currMagnusSpace.GetChild(0);
        if (playerCurrMagnus.childCount == 0) {
            selectedMagnus.transform.position = currMagnusSpace.position;
            selectedMagnus.transform.SetParent(playerCurrMagnus, true);
            selectedMagnus.transform.localScale = new Vector3(1,1,1);
        }
        else {
            selectedMagnus.transform.position = nextMagnusSpace.position;
            selectedMagnus.transform.SetParent(nextMagnusSpace, true);
            selectedMagnus.transform.localScale = new Vector3(1, 1, 1);

            SetCanSelect(false); //Magnus queue is full, wait to select more
        }
        cards.RemoveAt(currentPosition);

        //selected card not rightmost card in hand, shift cards on right normally and cursor position stays the same
        if (currentPosition != cards.Count) {
            ShiftCards(currentPosition, cards.Count - 1);
            MoveToPosition(currentPosition);
        }

        bool cardDrawn = DrawCard();

        //cursor was rightmost, select newly drawn card
        if (cardDrawn && currentPosition == cards.Count - 1) {
            MoveToPosition(currentPosition);
        }

        //cursor was rightmost and no new card drawn, move cursor left
        if (!cardDrawn && currentPosition == cards.Count) {
            //no more cards in hand
            if (currentPosition == 0) {
                SetDescription(null);
                Hide();
            }
            else {
                MoveToPosition(currentPosition - 1);
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
