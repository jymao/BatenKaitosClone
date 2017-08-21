using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour {

    //Timer
    private bool firstMoveDone = false;
    private float firstMoveTimeLimit = 5f;
    private const float TIME_LIMIT_INTERVAL = 0.1f;
    private float lastTimeInterval = 0;

    //Magnus processing time
    private const float CARD_DELAY = 2f;
    private float lastTimeProcessed = 0;

    private bool isGameOver = false;
    private bool isPlayerTurn = true;
    private bool turnStarted = false;
    private bool turnDone = false;
    private bool shuffleTurn = false; //player turn spent shuffling cards

    private bool playedAttack = false;
    private bool playedHeal = false;
    private bool playedInvalid = false;
    private bool playedFinisher = false;

    private int enemyNumAttacks = 0;
    private int currCombo = 1;
    private int finalDamage = 0;
    private Dictionary<Element, int> elementDamage = new Dictionary<Element,int>();
    private Dictionary<Element, int> enemyElementDamage = new Dictionary<Element, int>();

    private string[] magnusList; //lines of MagnusList.txt
    private List<GameObject> deck = new List<GameObject>();
    private List<GameObject> graveyard = new List<GameObject>(); //previously played cards
    private List<PlayedMagnus> playedMagnus = new List<PlayedMagnus>();

    public GameObject magnusPrefab;
    public GameObject enemyMagnusPrefab;

    public Player player;
    public Enemy enemy;

    public Hand handCursor;
    public Transform deckSpace; //cards in deck and graveyard hidden off-screen
    public Transform playedMagnusSpace; //entry point for played magnus
    public Transform currMagnusSpace; //space for currently processed magnus
    public Transform nextMagnusSpace; //space for queued magnus
    public Transform timeLimit; //first move timer
    public Transform deckCapacityBar;
    public Transform message; //game messages (shuffling deck, victory, loss)
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
        deck.Add(CreateMagnus("Lord_of_the_Wind"));
        deck.Add(CreateMagnus("Scale_Mail"));
        deck.Add(CreateMagnus("Skull_Mask"));
        deck.Add(CreateMagnus("Fangs_of_Light"));

        for (int i = 0; i < 5; i++) {
            deck.Add(CreateMagnus("Solar_Saber"));
        }
        for (int i = 0; i < 5; i++) {
            deck.Add(CreateMagnus("Dimension_Blade"));
        }
        for (int i = 0; i < 5; i++) {
            deck.Add(CreateMagnus("Marvelous_Sword"));
        }
        for (int i = 0; i < 5; i++) {
            deck.Add(CreateMagnus("Blood_Sword"));
        }
        for (int i = 0; i < 5; i++) {
            deck.Add(CreateMagnus("Cetaka's_Sword"));
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
            //First move timer
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
            //Process enemy attacks and player defenses
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

    //Set Magnus' valid state based on current battle round
    public void ValidateCard(GameObject card) {
        if (card != null) {
            Magnus cardScript = card.GetComponent<Magnus>();

            //All cards are invalid if an invalid card was played
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

            //TODO: animate capacity bar
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
        //Move currently processed Magnus to played Magnus space
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

        //Move next queued Magnus to current Magnus space
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

    //Move played cards over to make room for next processed Magnus
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
        if (!shuffleTurn) {
            ResetBattleResults();
            HandleDamage();
            HandlePrizes();

            //Print final damage and apply effect
            Text finalDamageText = battleResults.GetChild(4).GetComponent<Text>();
            finalDamageText.text = "Final Damage: " + finalDamage;
            if(isPlayerTurn) {
                if (playedHeal) {
                    finalDamageText.color = new Color(0f / 255f, 255f / 255f, 65f / 255f);
                    player.IncreaseHealth(finalDamage);
                }
                else {
                    finalDamageText.color = new Color(221f / 255f, 208f / 255f, 155f / 255f);
                    enemy.DecreaseHealth(finalDamage);
                }
            }
            else {
                finalDamageText.color = new Color(221f / 255f, 208f / 255f, 155f / 255f);
                player.DecreaseHealth(finalDamage);
            }

            //show results for battle turns
            battleResults.gameObject.SetActive(true);
        }

        turnDone = true;

        handCursor.Hide();
        message.gameObject.SetActive(false);
        deckCapacityBar.gameObject.SetActive(false);
        timeLimit.gameObject.SetActive(false);
        handCursor.SetCanSelect(false);
        turnStarted = false;

        //prevents card from being selected when trying to close results screen
        handCursor.SetTurnEnded(true);

        isPlayerTurn = !isPlayerTurn;
    }

    private void ResetBattleResults() {
        for (int i = 1; i < 4; i++) {
            Transform tableEntry = battleResults.GetChild(i);
            for (int j = 0; j < 4; j++) {
                Transform elementEntry = tableEntry.GetChild(j);
                elementEntry.GetComponent<Text>().text = "";
            }
        }
    }

    //Calculate damage and battle results for the round
    private void HandleDamage() {
        //player and enemy attack rounds
        if (playedAttack || !isPlayerTurn) {
            //sum up player elemental damage/defense
            for (int i = 0; i < playedMagnus.Count; i++) {
                if (playedMagnus[i].magnus.GetIsValid()) {
                    if (playedMagnus[i].magnus.GetElement() != Element.Neutral) {
                        elementDamage[playedMagnus[i].magnus.GetElement()] += playedMagnus[i].magnus.GetElementStat();
                    }

                    elementDamage[Element.Neutral] += playedMagnus[i].magnus.GetNeutralStat();
                }
            }

            //print to offense table and cancel out elements
            HandleOffense();

            //print and apply enemy defense
            HandleDefense();

            //print to results table and apply elemental bonus damage
            HandleResults();

            //total damage for prize later
            if (isPlayerTurn) {
                finalDamage += elementDamage[Element.Neutral];
                finalDamage += elementDamage[Element.Fire];
                finalDamage += elementDamage[Element.Water];
                finalDamage += elementDamage[Element.Light];
                finalDamage += elementDamage[Element.Dark];
                finalDamage += elementDamage[Element.Wind];
                finalDamage += elementDamage[Element.Chronos];
            }
            else {
                finalDamage += enemyElementDamage[Element.Neutral];
                finalDamage += enemyElementDamage[Element.Fire];
                finalDamage += enemyElementDamage[Element.Water];
                finalDamage += enemyElementDamage[Element.Light];
                finalDamage += enemyElementDamage[Element.Dark];
                finalDamage += enemyElementDamage[Element.Wind];
                finalDamage += enemyElementDamage[Element.Chronos];
            }
        }
        else if (playedHeal) {
            for (int i = 0; i < playedMagnus.Count; i++) {
                if (playedMagnus[i].magnus.GetIsValid()) {
                    finalDamage += playedMagnus[i].magnus.GetNeutralStat();
                }
            }
        }  
    }

    private void HandleOffense() {
        Transform offenseTable = battleResults.GetChild(1);

        if (isPlayerTurn) {
            //Neutral damage
            if (elementDamage[Element.Neutral] > 0) {
                offenseTable.GetChild(0).GetComponent<Text>().text = "Neutral: " + elementDamage[Element.Neutral];
            }

            //Fire or Water
            PrintPlayerElements(Element.Fire, Element.Water, 1, false);

            //Light or Dark
            PrintPlayerElements(Element.Light, Element.Dark, 2, false);

            //Wind or Chronos
            PrintPlayerElements(Element.Wind, Element.Chronos, 3, false);

        }
        else {
            //Neutral
            offenseTable.GetChild(0).GetComponent<Text>().text = "Neutral: " + enemy.GetAttack();
            enemyElementDamage[Element.Neutral] = enemy.GetAttack();

            //Fire or Water
            HandleEnemyOffense(Element.Fire, Element.Water, 1);

            //Light or Dark
            HandleEnemyOffense(Element.Light, Element.Dark, 2);

            //Wind or Chronos
            HandleEnemyOffense(Element.Wind, Element.Chronos, 3);
        }
    }

    //Determine which player element in a pair (ex: Water vs Fire) is stronger and print it.
    //Cancel out some of the element with the opposing element if both were played.
    private void PrintPlayerElements(Element elem1, Element elem2, int index, bool isDefense) {
        Transform elementEntry = battleResults.GetChild(1).GetChild(index);
        if (isDefense) {
            elementEntry = battleResults.GetChild(2).GetChild(index);
        }

        Text entryText = elementEntry.GetComponent<Text>();

        //Print row if either element is played
        if (elementDamage[elem1] > 0 || elementDamage[elem2] > 0) {
            //First element is stronger
            if (elementDamage[elem1] >= elementDamage[elem2]) {
                //No opposing element, only print first element
                if (elementDamage[elem2] == 0) {
                    entryText.text = elem1.ToString() + ": " + elementDamage[elem1];
                }
                //Opposing element was also played, print amount for both
                else {
                    entryText.text = elem1.ToString() + ": " + elementDamage[elem1] + " (" + elem2.ToString() + ": " + elementDamage[elem2] + ")";
                }

                //Cancel out difference
                elementDamage[elem1] -= elementDamage[elem2];
                elementDamage[elem2] = 0;

                //Apply defense to enemy attacks
                if (isDefense) {
                    ApplyDefense(elem1, elem2);
                }
            }
            //Second element is stronger
            else {
                if (elementDamage[elem1] == 0) {
                    entryText.text = elem2.ToString() + ": " + elementDamage[elem2];
                }
                else {
                    entryText.text = elem2.ToString() + ": " + elementDamage[elem2] + " (" + elem1.ToString() + ": " + elementDamage[elem1] + ")";
                }

                elementDamage[elem2] -= elementDamage[elem1];
                elementDamage[elem1] = 0;

                //Apply defense to enemy attacks
                if (isDefense) {
                    ApplyDefense(elem2, elem1);
                }
            }
        }
    }

    //Determine which element out of a pair enemy uses for attack
    private void HandleEnemyOffense(Element elem1, Element elem2, int index) {
        Transform offenseTable = battleResults.GetChild(1);

        //Negative affinity means player attacks using that element are less effective on the enemy,
        //so the enemy uses that element.
        if (enemy.GetAffinity(elem1) < 0) {
            offenseTable.GetChild(index).GetComponent<Text>().text = elem1.ToString() + ": " + enemy.GetElementAttack();
            enemyElementDamage[elem1] = enemy.GetElementAttack();
        }
        else if (enemy.GetAffinity(elem2) < 0) {
            offenseTable.GetChild(index).GetComponent<Text>().text = elem2.ToString() + ": " + enemy.GetElementAttack();
            enemyElementDamage[elem2] = enemy.GetElementAttack();
        }
    }

    private void HandleDefense() {
        Transform defenseTable = battleResults.GetChild(2);

        if (!isPlayerTurn) {
            //Neutral damage
            if (elementDamage[Element.Neutral] > 0) {
                defenseTable.GetChild(0).GetComponent<Text>().text = "Neutral: " + elementDamage[Element.Neutral];
            }
            ApplyDefense(Element.Neutral, Element.Neutral);

            //Fire or Water
            PrintPlayerElements(Element.Fire, Element.Water, 1, true);

            //Light or Dark
            PrintPlayerElements(Element.Light, Element.Dark, 2, true);

            //Wind or Chronos
            PrintPlayerElements(Element.Wind, Element.Chronos, 3, true);

        }
        else {
            //Neutral
            defenseTable.GetChild(0).GetComponent<Text>().text = "Neutral: " + enemy.GetDefense();
            enemyElementDamage[Element.Neutral] = enemy.GetDefense();
            ApplyDefense(Element.Neutral, Element.Neutral);

            //Fire or Water
            HandleEnemyDefense(Element.Fire, Element.Water, 1);

            //Light or Dark
            HandleEnemyDefense(Element.Light, Element.Dark, 2);

            //Wind or Chronos
            HandleEnemyDefense(Element.Wind, Element.Chronos, 3);
        }
    }

    //Determine which element enemy uses for defense and apply defense to player attack
    private void HandleEnemyDefense(Element elem1, Element elem2, int index) {
        Transform defenseTable = battleResults.GetChild(2);

        //Negative affinity means player attacks using that element are less effective on the enemy,
        //so the enemy uses that element.
        if (enemy.GetAffinity(elem1) < 0) {
            defenseTable.GetChild(index).GetComponent<Text>().text = elem1.ToString() + ": " + enemy.GetElementDefense();
            enemyElementDamage[elem1] = enemy.GetElementDefense();
            ApplyDefense(elem2, elem1);
        }
        else if (enemy.GetAffinity(elem2) < 0) {
            defenseTable.GetChild(index).GetComponent<Text>().text = elem2.ToString() + ": " + enemy.GetElementDefense();
            enemyElementDamage[elem2] = enemy.GetElementDefense();
            ApplyDefense(elem1, elem2);
        }
    }

    private void ApplyDefense(Element playerElement, Element enemyElement) {
        if (isPlayerTurn) {
            elementDamage[playerElement] -= enemyElementDamage[enemyElement];
            if (elementDamage[playerElement] < 0) {
                elementDamage[playerElement] = 0;
            }
        }
        else {
            enemyElementDamage[enemyElement] -= elementDamage[playerElement];
            if (enemyElementDamage[enemyElement] < 0) {
                enemyElementDamage[enemyElement] = 0;
            }
        }
    }

    private void HandleResults() {
        Transform resultsTable = battleResults.GetChild(3);

        if (isPlayerTurn) {
            if (elementDamage[Element.Neutral] > 0) {
                resultsTable.GetChild(0).GetComponent<Text>().text = "Neutral: " + elementDamage[Element.Neutral];
            }
        }
        else {
            if (enemyElementDamage[Element.Neutral] > 0) {
                resultsTable.GetChild(0).GetComponent<Text>().text = "Neutral: " + enemyElementDamage[Element.Neutral];
            }
        }

        PrintResults(Element.Fire, Element.Water, 1);
        PrintResults(Element.Light, Element.Dark, 2);
        PrintResults(Element.Wind, Element.Chronos, 3);
    }

    //Print the used element and its damage for each results row. Add or subtract damage
    //based on enemy elements for player attacks.
    private void PrintResults(Element elem1, Element elem2, int index) {
        Transform resultsTable = battleResults.GetChild(3);

        if (isPlayerTurn) {
            if (elementDamage[elem1] > 0) {
                resultsTable.GetChild(index).GetComponent<Text>().text = elem1.ToString() + ": " + elementDamage[elem1];

                if (enemy.GetAffinity(elem1) > 0) {
                    resultsTable.GetChild(index).GetComponent<Text>().text += "   +" + enemy.GetAffinity(elem1) + "%";
                }
                else if (enemy.GetAffinity(elem1) < 0) {
                    resultsTable.GetChild(index).GetComponent<Text>().text += "   " + enemy.GetAffinity(elem1) + "%";
                }

                float affinityPercentage = ((float)enemy.GetAffinity(elem1)) / 100f;
                int bonusDamage = (int)(elementDamage[elem1] * affinityPercentage);
                elementDamage[elem1] += bonusDamage;
                if (elementDamage[elem1] < 0) {
                    elementDamage[elem1] = 0;
                }
            }
            else if (elementDamage[elem2] > 0) {
                resultsTable.GetChild(index).GetComponent<Text>().text = elem2.ToString() + ": " + elementDamage[elem2];

                if (enemy.GetAffinity(elem2) > 0) {
                    resultsTable.GetChild(index).GetComponent<Text>().text += "   +" + enemy.GetAffinity(elem2) + "%";
                }
                else if (enemy.GetAffinity(elem2) < 0) {
                    resultsTable.GetChild(index).GetComponent<Text>().text += "   " + enemy.GetAffinity(elem2) + "%";
                }

                float affinityPercentage = ((float)enemy.GetAffinity(elem2)) / 100f;
                int bonusDamage = (int)(elementDamage[elem2] * affinityPercentage);
                elementDamage[elem2] += bonusDamage;
                if (elementDamage[elem2] < 0) {
                    elementDamage[elem2] = 0;
                }
            }
        }
        else {
            if (enemyElementDamage[elem1] > 0) {
                resultsTable.GetChild(index).GetComponent<Text>().text = elem1.ToString() + ": " + enemyElementDamage[elem1];
            }
            else if (enemyElementDamage[elem2] > 0) {
                resultsTable.GetChild(index).GetComponent<Text>().text = elem2.ToString() + ": " + enemyElementDamage[elem2];
            }
        }
    }

    //Determine prize based on played Magnus' spirit numbers and apply bonus to final damage
    private void HandlePrizes() {
        foreach (Transform child in prizeArea) {
            Destroy(child.gameObject);
        }
        List<Prize> prizes = PrizePercentageCalculator.instance.CalculateBonus(playedMagnus);
        foreach (Prize prize in prizes) {
            Transform element = Instantiate(prizeElementPrefab, prizeArea);
            element.Find("PrizeName").GetComponent<Text>().text = prize.name;
            
            //Print prize and apply prize to final damage.
            //Prizes are multiplied together instead of applied to original damage. Not sure if the original game does the same.
            if (isPlayerTurn) {
                element.Find("PrizePercentage").GetComponent<Text>().text = "+" + (prize.offense * 100 - 100) + "%";
                finalDamage = (int)((float)finalDamage * prize.offense);
            }
            else {
                element.Find("PrizePercentage").GetComponent<Text>().text = (prize.defense * 100 - 100) + "%";
                finalDamage = (int)((float)finalDamage * prize.defense);
            }
        }
        playedMagnus.Clear();
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

            ResetElementDamage();
            finalDamage = 0;

            playedAttack = false;
            playedHeal = false;
            playedInvalid = false;
            playedFinisher = false;

            handCursor.ValidateHand();
            handCursor.SetCanSelect(true);
            handCursor.ResetNumCardsPlayed();
        }
    }

    private void ResetElementDamage() {
        elementDamage[Element.Neutral] = 0;
        elementDamage[Element.Fire] = 0;
        elementDamage[Element.Water] = 0;
        elementDamage[Element.Light] = 0;
        elementDamage[Element.Dark] = 0;
        elementDamage[Element.Wind] = 0;
        elementDamage[Element.Chronos] = 0;

        enemyElementDamage[Element.Neutral] = 0;
        enemyElementDamage[Element.Fire] = 0;
        enemyElementDamage[Element.Water] = 0;
        enemyElementDamage[Element.Light] = 0;
        enemyElementDamage[Element.Dark] = 0;
        enemyElementDamage[Element.Wind] = 0;
        enemyElementDamage[Element.Chronos] = 0;
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
