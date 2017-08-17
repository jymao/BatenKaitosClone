using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour {

    private bool firstMoveDone = false;
    private float firstMoveTimeLimit = 5f;
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

    private bool playedAttack = false;
    private bool playedHeal = false;
    private bool playedInvalid = false;
    private bool playedFinisher = false;

    private int enemyNumAttacks = 0;
    private int currCombo = 1;

    private string[] magnusList;
    private List<GameObject> deck = new List<GameObject>();
    private List<GameObject> graveyard = new List<GameObject>();
    private List<PlayedMagnus> playedMagnus = new List<PlayedMagnus>();

    public GameObject magnusPrefab;
    public GameObject enemyMagnusPrefab;

    public Enemy enemy;
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
    public int GetEnemyNumAttacks() { return enemyNumAttacks; }

    public void SetFirstMoveDone(bool b) { firstMoveDone = b; }
    public void SetTurnStarted(bool b) { turnStarted = b; }
    public void SetTimeProcessed() { lastTimeProcessed = Time.time; }
    public void PlayMagnus(PlayedMagnus magnus) { 
        playedMagnus.Add(magnus);
        if (!playedHeal && !playedAttack) {
            if (magnus.magnus.GetIsAtk()) {
                playedAttack = true;
            }
            else if (magnus.magnus.GetIsHeal()) {
                playedHeal = true;
            }
        }

        playedFinisher = magnus.magnus.GetIsFinisher();

        if (magnus.magnus.GetIsValid()) {
            currCombo++;
        }
        else {
            playedInvalid = true;
        }
    }

    // Use this for initialization
    void Start() {
        TextAsset magnusListAsset = Resources.Load<TextAsset>("MagnusList");

        string[] newline = { Environment.NewLine };
        //char[] newline = { '\n' };
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

        for (int i = 0; i < 5; i++) {
            deck.Add(CreateMagnus("Cherries"));
        }
        for (int i = 0; i < 5; i++) {
            deck.Add(CreateMagnus("Dimension_Blade"));
        }

        ShuffleDeck();
        StartCoroutine(StartTurn());

        handCursor.Initialize();

    }

    // Update is called once per frame
    void Update() {

        if (Input.GetKeyDown(KeyCode.Space) && battleResults.gameObject.activeInHierarchy) {
            StartCoroutine(StartTurn());
        }

        if (isPlayerTurn) {
            if (turnStarted && !firstMoveDone) {
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
            else if (firstMoveDone) {
                timeLimit.gameObject.SetActive(false);
            }

            //Process attacks
            if (firstMoveDone && !turnDone) {
                if (Time.time - lastTimeProcessed > CARD_DELAY) {
                    if (!playedInvalid && !playedFinisher) {
                        handCursor.SetCanSelect(true);
                    }
                    ProcessMagnus();
                    lastTimeProcessed = Time.time;
                }
            }
        }
        else {
            if (turnStarted && !turnDone) {
                if (Time.time - lastTimeProcessed > CARD_DELAY) {
                    if (enemyNumAttacks > 0) {
                        EnemyAttack();
                    }
                    handCursor.SetCanSelect(true);
                    ProcessMagnus();
                    lastTimeProcessed = Time.time;
                }
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

    public void ValidateCard(GameObject card) {
        if (card != null) {
            Magnus cardScript = card.GetComponent<Magnus>();

            if (playedInvalid) {
                cardScript.SetValid(false);
                return;
            }

            if (isPlayerTurn) {
                //def only magnus invalid
                if (cardScript.GetIsDef() && !cardScript.GetIsAtk()) {
                    cardScript.SetValid(false);
                }
                //atk combo too high
                else if (cardScript.GetAtkCombo() > currCombo) {
                    cardScript.SetValid(false);
                }
                //heal magnus invalid after attacking or attack magnus after healing
                else if((cardScript.GetIsHeal() && playedAttack) || (cardScript.GetIsAtk() && playedHeal)) {
                    cardScript.SetValid(false);
                }
                else {
                    cardScript.SetValid(true);
                }
            }
            else {
                //atk only magnus invalid
                //heal magnus invalid
                if (!cardScript.GetIsDef()) {
                    cardScript.SetValid(false);
                }
                //def combo too high
                else if(cardScript.GetDefCombo() > currCombo) {
                    cardScript.SetValid(false);
                }
                else {
                    cardScript.SetValid(true);
                }
            }
        }
    }

    public GameObject DrawCard() {
        GameObject card = null;
        if (deck.Count != 0) {
            card = deck[0];
            deck.RemoveAt(0);

            ValidateCard(card);

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
        Transform playerPlayedMagnus = playedMagnusSpace.GetChild(0);
        Transform enemyPlayedMagnus = playedMagnusSpace.GetChild(1);

        while (playerPlayedMagnus.childCount > 0) {
            GameObject card = playerPlayedMagnus.GetChild(0).gameObject;
            card.GetComponent<Magnus>().Reset();
            card.transform.SetParent(null, true);
            card.transform.localScale = new Vector3(1, 1, 1);
            Discard(card);
        }

        
        while (enemyPlayedMagnus.childCount > 0) {
            GameObject card = enemyPlayedMagnus.GetChild(0).gameObject;
            card.transform.SetParent(null, true); //still necessary despite destroy
            Destroy(card);
        }
        
    }

    private void ReInitDeck() {
        while(graveyard.Count > 0) {
            deck.Add(graveyard[0]);
            graveyard.RemoveAt(0);
        }
    }

    private void ProcessMagnus() {
        Transform playerPlayedMagnus = playedMagnusSpace.GetChild(0);
        Transform enemyPlayedMagnus = playedMagnusSpace.GetChild(1);
        Transform playerCurrMagnus = currMagnusSpace.GetChild(0);
        Transform enemyCurrMagnus = currMagnusSpace.GetChild(1);

        ShiftPlayedCards();
        if (playerCurrMagnus.childCount != 0) {
            GameObject currentMagnus = playerCurrMagnus.GetChild(0).gameObject;
            currentMagnus.transform.SetParent(playerPlayedMagnus, true);
            currentMagnus.transform.position = playedMagnusSpace.position;
            currentMagnus.transform.localScale = new Vector3(1, 1, 1);
        }
        if (enemyCurrMagnus.childCount != 0) {
            GameObject currentMagnus = enemyCurrMagnus.GetChild(0).gameObject;
            currentMagnus.transform.SetParent(enemyPlayedMagnus, true);
            currentMagnus.transform.position = playedMagnusSpace.position;
            currentMagnus.transform.localScale = new Vector3(1, 1, 1);
        }

        if (nextMagnusSpace.childCount != 0) {
            GameObject nextMagnus = nextMagnusSpace.GetChild(0).gameObject;
            nextMagnus.transform.SetParent(playerCurrMagnus, true);
            nextMagnus.transform.position = currMagnusSpace.position;
        }

        //no more cards left in queue, end turn
        if (playerCurrMagnus.childCount == 0 && isPlayerTurn) {
            EndTurn();
        }
        else if (enemyCurrMagnus.childCount == 0 && !isPlayerTurn) {
            EndTurn();
        }
    }

    private void ShiftPlayedCards() {
        Transform playerPlayedMagnus = playedMagnusSpace.GetChild(0);
        Transform enemyPlayedMagnus = playedMagnusSpace.GetChild(1);

        if (playerPlayedMagnus.childCount > 0) {
            float magnusWidth = playerPlayedMagnus.GetChild(0).GetComponent<Magnus>().GetWidth();

            for (int i = 0; i < playerPlayedMagnus.childCount; i++) {
                Vector3 origPosition = playerPlayedMagnus.GetChild(i).position;
                playerPlayedMagnus.GetChild(i).position = new Vector3(origPosition.x + magnusWidth, origPosition.y, origPosition.z);
            }
        }

        if (enemyPlayedMagnus.childCount > 0) {
            Transform cardGraphic = enemyPlayedMagnus.GetChild(0).GetChild(0);
            float magnusWidth = cardGraphic.GetComponent<SpriteRenderer>().sprite.bounds.size.x * cardGraphic.localScale.x;
            Transform ancestor = enemyPlayedMagnus.parent;
            while (ancestor != null) {
                magnusWidth *= ancestor.localScale.x;
                ancestor = ancestor.parent;
            }

            for (int i = 0; i < enemyPlayedMagnus.childCount; i++) {
                Vector3 origPosition = enemyPlayedMagnus.GetChild(i).position;
                enemyPlayedMagnus.GetChild(i).position = new Vector3(origPosition.x + magnusWidth, origPosition.y, origPosition.z);
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

        isPlayerTurn = !isPlayerTurn;
    }

    private void HandlePrizes() {
        foreach (Transform child in prizeArea) {
            Destroy(child.gameObject);
        }
        List<Prize> prizes = PrizePercentageCalculator.instance.CalculateBonus(playedMagnus);
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
        if (deck.Count == 0 && isPlayerTurn) {
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

            if (isPlayerTurn) {
                firstMoveDone = false;
                firstMoveTimeLimit = 5f;
                timeLimit.gameObject.SetActive(true);
                timeLimit.GetChild(1).GetComponent<Text>().text = "05.0";
            }
            else {
                enemyNumAttacks = enemy.NumAttacks();
                EnemyAttack();
                lastTimeProcessed = Time.time;
            }

            turnStarted = true;
            turnDone = false;
            currCombo = 1;
            playedAttack = false;
            playedHeal = false;
            playedInvalid = false;
            playedFinisher = false;
            handCursor.ValidateHand();
            handCursor.SetCanSelect(true);
            handCursor.ResetNumCardsPlayed();
        }
    }

    private void EnemyAttack() {
        GameObject enemyCard = (GameObject)Instantiate(enemyMagnusPrefab, Vector3.zero, Quaternion.identity);
        Transform enemyCurrMagnus = currMagnusSpace.GetChild(1);
        enemyCard.transform.SetParent(enemyCurrMagnus, true);
        enemyCard.transform.position = enemyCurrMagnus.position;
        enemyCard.transform.localScale = new Vector3(1, 1, 1);

        enemyNumAttacks--;
    }
}
