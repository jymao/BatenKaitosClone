using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour {

    public float turnTimeLimit = 30f;
    private bool firstMoveDone = false;
    private float firstMoveTimeLimit = 30f;
    private const float TIME_LIMIT_INTERVAL = 0.1f;
    private float lastTimeInterval = 0;

    private const float CARD_DELAY = 2f;
    private float lastTimeProcessed = 0;

    private bool gameStarted = false;
    private bool isGameOver = false;
    private bool isPlayerTurn = true;
    private bool turnStarted = false;
    private bool turnDone = false;
    private bool shuffleTurn = false;

    private string[] magnusList;
    private List<GameObject> deck = new List<GameObject>();
    private List<GameObject> graveyard = new List<GameObject>();
    private List<PlayedMagnus> playedMagnus = new List<PlayedMagnus>();

    public GameObject magnusPrefab;
    public Hand handCursor;
    public Transform deckSpace;
    public Transform playedMagnusSpace;
    public Transform currMagnusSpace;
    public Transform nextMagnusSpace;
    public Transform timeLimit;
    public Transform deckCapacityBar;
    public Transform message;
    public Transform battleResults;
    public Transform prizeArea;
    public Transform prizeElementPrefab;

    public bool GetIsPlayerTurn() { return isPlayerTurn; }
    public bool GetFirstMoveDone() { return firstMoveDone; }

    public void SetFirstMoveDone(bool b) { firstMoveDone = b; }
    public void SetTurnStarted(bool b) { turnStarted = b; }
    public void SetTimeProcessed() { lastTimeProcessed = Time.time; }
    public void playMagnus(PlayedMagnus magnus) { playedMagnus.Add(magnus); }

    // Use this for initialization
    void Start() {
        TextAsset magnusListAsset = Resources.Load<TextAsset>("MagnusList");

        string[] newline = { Environment.NewLine };
        magnusList = magnusListAsset.text.Split(newline, StringSplitOptions.RemoveEmptyEntries);

        deck.Add(CreateMagnus("Beer"));
        deck.Add(CreateMagnus("Air_Slash"));
        deck.Add(CreateMagnus("Sea_Bream"));
        deck.Add(CreateMagnus("Saber"));
        deck.Add(CreateMagnus("Blue_Storm"));
        deck.Add(CreateMagnus("Flametongue"));
        deck.Add(CreateMagnus("Holy_Armor"));
        deck.Add(CreateMagnus("Gladius"));
        deck.Add(CreateMagnus("Water_Blade"));

        for (int i = 0; i < 10; i++) {
            deck.Add(CreateMagnus("Cherries"));
        }

        ShuffleDeck();
        StartCoroutine(StartTurn());

        handCursor.initialize();
    }

    // Update is called once per frame
    void Update() {

        if (Input.GetKeyDown(KeyCode.Space) && battleResults.gameObject.activeInHierarchy) {
            StartCoroutine(StartTurn());
        }

        if (turnStarted && isPlayerTurn && !firstMoveDone) {
            if (Time.time - lastTimeInterval > TIME_LIMIT_INTERVAL && firstMoveTimeLimit >= 0.1) {
                firstMoveTimeLimit -= 0.1f;
                timeLimit.GetChild(1).GetComponent<Text>().text = "0" + firstMoveTimeLimit.ToString("0.0");
                lastTimeInterval = Time.time;

                //if timer runs out, end turn
                if (firstMoveTimeLimit < 0.1) {
                    EndTurn();
                }
            }
        }
        else if (isPlayerTurn && firstMoveDone) {
            timeLimit.gameObject.SetActive(false);
        }

        //Process attacks
        if (firstMoveDone && !turnDone) {
            if (Time.time - lastTimeProcessed > CARD_DELAY) {
                handCursor.SetCanSelect(true);
                ProcessMagnus();
                lastTimeProcessed = Time.time;
            }
        }
    }

    private GameObject CreateMagnus(string name) {
        GameObject magnus = (GameObject)Instantiate(magnusPrefab, deckSpace.position, Quaternion.identity);
        Magnus magnusScript = magnus.GetComponent<Magnus>();

        for (int i = 0; i < magnusList.Length; i++) {

            if (magnusList[i] == name) {
                magnusScript.SetName(name);
                magnusScript.SetIsAtk(Convert.ToBoolean(GetPropertyValue(magnusList[++i])));
                magnusScript.SetIsDef(Convert.ToBoolean(GetPropertyValue(magnusList[++i])));
                magnusScript.SetIsHeal(Convert.ToBoolean(GetPropertyValue(magnusList[++i])));
                magnusScript.SetIsFinisher(Convert.ToBoolean(GetPropertyValue(magnusList[++i])));
                magnusScript.SetNeutralStat(Convert.ToInt32(GetPropertyValue(magnusList[++i])));
                magnusScript.SetElementStat(Convert.ToInt32(GetPropertyValue(magnusList[++i])));
                magnusScript.SetAtkCombo(Convert.ToInt32(GetPropertyValue(magnusList[++i])));
                magnusScript.SetDefCombo(Convert.ToInt32(GetPropertyValue(magnusList[++i])));
                magnusScript.SetElement(GetPropertyValue(magnusList[++i]));
                
                Sprite s = Resources.Load<Sprite>("Images/Magnus/" + name);
                magnusScript.SetGraphic(s);

                break;
            }
        }

        return magnus;
    }

    //Helper method for CreateMagnus
    private string GetPropertyValue(string line) {
        char[] space = { ' ' };
        string[] words = line.Split(space, StringSplitOptions.RemoveEmptyEntries);
        return words[2];
    }

    //Shuffle Magnus deck using the Fisher-Yates shuffle algorithm
    private void ShuffleDeck() {
        int count = deck.Count;
        while (count > 1) {
            count--;
            int k = UnityEngine.Random.Range(0, count+1);
            GameObject temp = deck[k];
            deck[k] = deck[count];
            deck[count] = temp;
        }
    }

    public GameObject DrawCard() {
        GameObject card = null;
        if (deck.Count != 0) {
            card = deck[0];
            deck.RemoveAt(0);

            string cardsRemaining = deck.Count.ToString();
            if (deck.Count < 10) {
                cardsRemaining = "0" + deck.Count.ToString();
            }
            deckCapacityBar.GetChild(2).GetComponent<Text>().text = cardsRemaining;
        }
        return card;
    }

    public void Discard(GameObject card) {
        graveyard.Add(card);
        card.GetComponent<Magnus>().Show(); //card was invisible due to hand being hidden
        card.transform.position = deckSpace.position;
    }

    private void DiscardPlayedMagnus() {
        while (playedMagnusSpace.childCount > 0) {
            GameObject card = playedMagnusSpace.GetChild(0).gameObject;
            card.GetComponent<Magnus>().Reset();
            card.transform.SetParent(null, true);
            card.transform.localScale = new Vector3(1, 1, 1);
            Discard(card);
        }
    }

    private void ReInitDeck() {
        while(graveyard.Count > 0) {
            deck.Add(graveyard[0]);
            graveyard.RemoveAt(0);
        }
    }

    private void ProcessMagnus() {
        GameObject currentMagnus = currMagnusSpace.GetChild(0).gameObject;
        ShiftPlayedCards();
        currentMagnus.transform.SetParent(playedMagnusSpace, true);
        currentMagnus.transform.position = playedMagnusSpace.position;
        currentMagnus.transform.localScale = new Vector3(1, 1, 1);

        if (nextMagnusSpace.childCount != 0) {
            GameObject nextMagnus = nextMagnusSpace.GetChild(0).gameObject;
            nextMagnus.transform.SetParent(currMagnusSpace, true);
            nextMagnus.transform.position = currMagnusSpace.position;
        }

        //no more cards left in queue, end turn
        if (currMagnusSpace.childCount == 0) {
            EndTurn();
        }
    }

    private void ShiftPlayedCards() {
        if (playedMagnusSpace.childCount > 0) {

            float magnusWidth = playedMagnusSpace.GetChild(0).GetComponent<Magnus>().GetWidth();

            for (int i = 0; i < playedMagnusSpace.childCount; i++) {
                Vector3 origPosition = playedMagnusSpace.GetChild(i).position;
                playedMagnusSpace.GetChild(i).position = new Vector3(origPosition.x + magnusWidth, origPosition.y, origPosition.z);
            }
        }
    }

    private void EndTurn() {
        HandlePrizes();

        turnDone = true;

        handCursor.Hide();
        message.gameObject.SetActive(false);
        deckCapacityBar.gameObject.SetActive(false);
        timeLimit.gameObject.SetActive(false);
        handCursor.SetCanSelect(false);
        turnStarted = false;

        //prevents card from being selected when trying to close results screen
        handCursor.SetTurnEnded(true);

        //show results for battle turns
        if (!shuffleTurn) {
            battleResults.gameObject.SetActive(true);
        }
    }

    private void HandlePrizes() {
        foreach (Transform child in prizeArea) {
            Destroy(child.gameObject);
        }
        List<Prize> prizes = PrizePercentageCalculator.instance.calculateBonus(playedMagnus);
        foreach (Prize prize in prizes) {
            Transform element = Instantiate(prizeElementPrefab, prizeArea);
            element.Find("PrizeName").GetComponent<Text>().text = prize.name;
            // TODO: Make applying offense or defense configurable. For now, just display offense
            element.Find("PrizePercentage").GetComponent<Text>().text = "+" + (prize.offense * 100 - 100) + "%";
        }
        playedMagnus.Clear();

        // TODO: use prize multipliers from above to apply damage bonus
    }

    private IEnumerator StartTurn() {
        DiscardPlayedMagnus();
        battleResults.gameObject.SetActive(false);

        //No cards in deck, spend a turn reshuffling the deck
        if (deck.Count == 0) {
            shuffleTurn = true;
            message.gameObject.SetActive(true);
            message.GetComponent<Text>().text = "Shuffling the Deck";
            yield return new WaitForSeconds(2f);

            EndTurn();
            handCursor.SetTurnEnded(false); //no results screen for shuffling turn, so cancel extra button input requirement to select a card
            handCursor.DiscardHand();
            ReInitDeck();
            ShuffleDeck();
            handCursor.NewHand();
            handCursor.Hide();

            StartCoroutine(StartTurn());
            shuffleTurn = false;
        }
        //Normal battle turn
        else {
            handCursor.Show();
            deckCapacityBar.gameObject.SetActive(true);
            firstMoveDone = false;
            firstMoveTimeLimit = turnTimeLimit;
            timeLimit.gameObject.SetActive(true);
            timeLimit.GetChild(1).GetComponent<Text>().text = "30.0";
            turnStarted = true;
            turnDone = false;
            handCursor.SetCanSelect(true);
            handCursor.ResetNumCardsPlayed();
        }
    }
}
