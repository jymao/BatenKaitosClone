using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class DeckEditor : MonoBehaviour {

    private const float GATHERING_LEFT_BOUND = -6.5f;
    private const float DECK_LEFT_BOUND = 1.35f;
    private const float TOP_BOUND = 2.2f;
    private const int NUM_COLUMNS = 4;

    private const float MOVE_DELAY = 0.125f;
    private float lastTimeMoved;

    private bool inGathering = true;
    private int topGatheringRow = 0; //Current top row shown in the gathering panel. Used for scrolling.
    private int topDeckRow = 0;
    private int currentRow = 0;
    private int currentCol = 0;

    private int deckCount = 0;
    private const int MAX_DECK_SIZE = 60;

    private List<List<GameObject>> gathering = new List<List<GameObject>>();
    private List<List<GameObject>> deck = new List<List<GameObject>>();

    private string[] magnusList; //lines of MagnusList.txt
    public GameObject magnusPrefab;

    public Button startButton;
    public Transform descriptionBar;
    public Text deckText;
    public Transform cursor;

	// Use this for initialization
	void Start () {
        TextAsset magnusListAsset = Resources.Load<TextAsset>("MagnusList");

        string[] newline = { Environment.NewLine };
        //char[] newline = { '\n' };
        magnusList = magnusListAsset.text.Split(newline, StringSplitOptions.RemoveEmptyEntries);

        InitializeGathering();
        MoveToPosition(0, 0);
	}
	
	// Update is called once per frame
	void Update () {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        // Allow us to move the cursor freely if we stop holding the button down
        if (inputX == 0 && inputY == 0) {
            lastTimeMoved = 0;
        }

        //Moving hand cursor
        if (Time.time - lastTimeMoved >= MOVE_DELAY) {
            if (inputX > 0) {
                //if in gathering section and not at gathering column limit or deck exists, can move right
                //if in deck section, can move right if column available
                if((inGathering && (currentCol < NUM_COLUMNS - 1 || deck.Count > 0)) 
                        || (!inGathering && currentCol < deck[currentRow].Count - 1)) {
                    MoveToPosition(currentRow, currentCol + 1);
                }
            }
            if (inputX < 0) {
                //if in gathering section, can move left if not at column 0
                //if in deck section, can always move left into gathering section
                if((inGathering && currentCol > 0) || !inGathering) {
                    MoveToPosition(currentRow, currentCol - 1);
                }
            }
            if (inputY > 0 && currentRow > 0) {
                MoveToPosition(currentRow - 1, currentCol);
            }
            if (inputY < 0) {
                //can move down a row if the section cursor is in has another row
                if((inGathering && currentRow < gathering.Count - 1) || (!inGathering && currentRow < deck.Count - 1)) {
                    MoveToPosition(currentRow + 1, currentCol);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            if (inGathering) {
                if (deckCount < MAX_DECK_SIZE) {
                    AddToDeck();
                }
            }
            else {
                RemoveFromDeck();
            }
        }

        if (deckCount < MAX_DECK_SIZE) {
            startButton.interactable = false;
        }
        else {
            startButton.interactable = true;
        }

	}

    private GameObject CreateMagnus(string name) {
        GameObject magnus = (GameObject)Instantiate(magnusPrefab, Vector3.zero, Quaternion.identity);
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

    private void InitializeGathering() {
        int row = 0;
        int col = 0;
        gathering.Add(new List<GameObject>());

        for (int line = 0; line < magnusList.Length; line += 10) {
            GameObject magnus = CreateMagnus(magnusList[line]);

            gathering[row].Add(magnus);
            magnus.transform.position = new Vector3(col * (magnus.GetComponent<Magnus>().GetWidth() + 0.1f) + GATHERING_LEFT_BOUND, 
                TOP_BOUND - (row * (magnus.GetComponent<Magnus>().GetHeight() + 0.5f)), 0);

            col++;
            //end of row, new row if there are remaining entries
            if (col == NUM_COLUMNS && line+10 < magnusList.Length) {
                col = 0;
                row++;
                gathering.Add(new List<GameObject>());
            }
        }
    }

    private void ShowCurrentRows() {
        for (int row = 0; row < gathering.Count; row++) {
            bool showRow = (row == topGatheringRow) || (row == topGatheringRow + 1);

            for (int col = 0; col < gathering[row].Count; col++) {
                Magnus magnus = gathering[row][col].GetComponent<Magnus>();
                if (showRow) {
                    magnus.Show();
                }
                else {
                    magnus.Hide();
                }
            }
        }

        for (int row = 0; row < deck.Count; row++) {
            bool showRow = (row == topDeckRow) || (row == topDeckRow + 1);

            for (int col = 0; col < deck[row].Count; col++) {
                Magnus magnus = deck[row][col].GetComponent<Magnus>();
                if (showRow) {
                    magnus.Show();
                }
                else {
                    magnus.Hide();
                }
            }
        }
    }

    private void MoveToPosition(int row, int col) {
        if (inGathering) {
            //move to deck section
            if (col == NUM_COLUMNS) {
                inGathering = false;
                currentCol = 0;

                //if deck has more than one row, move to row on same line.
                //if deck only has one row, move to top deck row.
                if (currentRow == topGatheringRow + 1 && deck.Count > 1) {
                    currentRow = topDeckRow + 1;
                }
                else {
                    currentRow = topDeckRow;
                }
            }
            //staying in gathering section
            else {
                currentRow = row;
                currentCol = col;

                //scrolling through gathering
                if (currentRow > topGatheringRow + 1) {
                    topGatheringRow++;
                    ShiftRows(true);
                }
                else if (currentRow < topGatheringRow) {
                    topGatheringRow--;
                    ShiftRows(false);
                }
            }
        }
        else {
            //move to gathering section
            if (col < 0) {
                inGathering = true;
                currentCol = NUM_COLUMNS - 1;

                //move to gathering row on same line as deck row
                if (currentRow == topDeckRow) {
                    currentRow = topGatheringRow;
                }
                else {
                    currentRow = topGatheringRow + 1;
                }
            }
            //staying in deck section
            else {
                currentRow = row;

                //if column exists in the new row, move there
                //otherwise move to last column on the new row
                if (col < deck[currentRow].Count) {
                    currentCol = col;
                }
                else {
                    currentCol = deck[currentRow].Count - 1;
                }

                //scrolling through deck section
                if (currentRow > topDeckRow + 1) {
                    topDeckRow++;
                    ShiftRows(true);
                }
                else if (currentRow < topDeckRow) {
                    topDeckRow--;
                    ShiftRows(false);
                }
            }
        }

        Vector3 magnusPos;
        if (inGathering) {
            magnusPos = gathering[currentRow][currentCol].transform.position;
        }
        else {
            magnusPos = deck[currentRow][currentCol].transform.position;
        }
        cursor.position = new Vector3(magnusPos.x, magnusPos.y + 1.3f, 0);
        ShowCurrentRows();
        SetDescription();

        lastTimeMoved = Time.time;
    }

    private void ShiftRows(bool shiftUp) {
        if (inGathering) {
            for (int row = 0; row < gathering.Count; row++) {
                for (int col = 0; col < gathering[row].Count; col++) {
                    Magnus magnusScript = gathering[row][col].GetComponent<Magnus>();

                    if (shiftUp) {
                        gathering[row][col].transform.position += new Vector3(0, magnusScript.GetHeight() + 0.5f, 0);
                    }
                    else {
                        gathering[row][col].transform.position -= new Vector3(0, magnusScript.GetHeight() + 0.5f, 0);
                    }
                }
            }
        }
        else {
            for (int row = 0; row < deck.Count; row++) {
                for (int col = 0; col < deck[row].Count; col++) {
                    Magnus magnusScript = deck[row][col].GetComponent<Magnus>();

                    if (shiftUp) {
                        deck[row][col].transform.position += new Vector3(0, magnusScript.GetHeight() + 0.5f, 0);
                    }
                    else {
                        deck[row][col].transform.position -= new Vector3(0, magnusScript.GetHeight() + 0.5f, 0);
                    }
                }
            }
        }
    }

    private void SetDescription() {
        Magnus magnus;
        if (inGathering) {
            magnus = gathering[currentRow][currentCol].GetComponent<Magnus>();
        }
        else {
            magnus = deck[currentRow][currentCol].GetComponent<Magnus>();
        }

        string magnusName = magnus.GetName().Replace("_", " ");
        string elementStat = magnus.GetElementStat().ToString();
        string totalStat = (magnus.GetNeutralStat() + magnus.GetElementStat()).ToString();
        string element = magnus.GetElement().ToString();

        string magnusEffect = "ATK/DEF ";
        if (magnus.GetIsHeal()) {
            magnusEffect = "Heals " + totalStat + " HP";
        }
        else {
            if (element == "Neutral") {
                magnusEffect += totalStat;
            }
            else {
                magnusEffect += totalStat + " (" + element + " " + elementStat + ")";
            }
        }

        string atkCombo = "ATK Combo: " + magnus.GetAtkCombo().ToString().Replace("0", "-");
        string defCombo = "DEF Combo: " + magnus.GetDefCombo().ToString().Replace("0", "-");

        descriptionBar.GetChild(0).GetComponent<Text>().text = magnusName;
        descriptionBar.GetChild(1).GetComponent<Text>().text = magnusEffect;
        descriptionBar.GetChild(2).GetComponent<Text>().text = atkCombo;
        descriptionBar.GetChild(3).GetComponent<Text>().text = defCombo;
    }

    private void AddToDeck() {
        if (deck.Count == 0) {
            deck.Add(new List<GameObject>());
        }

        List<GameObject> lastRow = deck[deck.Count - 1];
        //last row is full, add new row
        if (lastRow.Count == NUM_COLUMNS) {
            deck.Add(new List<GameObject>());
            lastRow = deck[deck.Count - 1];
        }

        //Add currently selected gathering card to deck, replace card in gathering with new card with same name
        lastRow.Add(gathering[currentRow][currentCol]);
        string magnusName = gathering[currentRow][currentCol].GetComponent<Magnus>().GetName();
        Vector3 magnusPos = gathering[currentRow][currentCol].transform.position;
        gathering[currentRow][currentCol] = CreateMagnus(magnusName);
        gathering[currentRow][currentCol].transform.position = magnusPos;

        //Proper position for card in deck
        Magnus magnusScript = lastRow[lastRow.Count - 1].GetComponent<Magnus>();
        float x = (lastRow.Count - 1) * (magnusScript.GetWidth() + 0.1f) + DECK_LEFT_BOUND;
        float y;
        if (deck[0].Count == 1) {
            y = TOP_BOUND - ((deck.Count - 1) * (magnusScript.GetHeight() + 0.5f));
        }
        //deck section might be scrolled down, so TOP_BOUND is incorrect
        else {
            y = deck[0][0].transform.position.y - ((deck.Count - 1) * (magnusScript.GetHeight() + 0.5f));
        }
        lastRow[lastRow.Count - 1].transform.position = new Vector3(x, y, 0);
        if (deck.Count - 1 > topDeckRow + 1) {
            magnusScript.Hide();
        }

        deckCount++;
        deckText.text = "Deck: " + deckCount + "/" + MAX_DECK_SIZE;
    }

    private void RemoveFromDeck() {
        GameObject cardToRemove = deck[currentRow][currentCol];
        Vector3 prevPosition = cardToRemove.transform.position;
        int prevRow = currentRow;
        int prevCol = currentCol;

        //Shift cards after card to remove
        for (int row = currentRow; row < deck.Count; row++) {
            //same row as card to remove, start from column after that card
            if (row == currentRow) {
                for (int col = currentCol + 1; col < deck[row].Count; col++) {
                    //set card to previous card's position and place in deck
                    Vector3 currPosition = deck[row][col].transform.position;
                    deck[row][col].transform.position = prevPosition;
                    deck[prevRow][prevCol] = deck[row][col];
                    prevPosition = currPosition;

                    //if card was moved to a visible row, show the card
                    if (prevRow == topDeckRow + 1) {
                        deck[prevRow][prevCol].GetComponent<Magnus>().Show();
                    }

                    prevCol = col;
                }
            }
            //rows below
            else {
                for (int col = 0; col < deck[row].Count; col++) {
                    //set card to previous card's position and place in deck
                    Vector3 currPosition = deck[row][col].transform.position;
                    deck[row][col].transform.position = prevPosition;
                    deck[prevRow][prevCol] = deck[row][col];
                    prevPosition = currPosition;

                    //if card was moved to a visible row, show the card
                    if (prevRow == topDeckRow + 1) {
                        deck[prevRow][prevCol].GetComponent<Magnus>().Show();
                    }

                    prevCol = col;

                    //previous row for next column is same row
                    if (col == 0) {
                        prevRow = row;
                    }
                }
            }
            prevRow = row;
        }
        Destroy(cardToRemove);

        //if cursor on last column (but not first column) of last row, move cursor left
        if (currentRow == deck.Count - 1 && currentCol == deck[deck.Count - 1].Count - 1 && currentCol != 0) {
            MoveToPosition(currentRow, currentCol - 1);
        }
        //Remove last deck entry (cursor either on last card, or cards were shifted over)
        deck[deck.Count - 1].RemoveAt(deck[deck.Count - 1].Count - 1);

        //remove last row if empty
        if (deck[deck.Count - 1].Count == 0) {
            //if cursor on same row as row to be removed
            if (currentRow == deck.Count - 1) {
                //if last row, move over to gathering
                if(deck.Count == 1) {
                    MoveToPosition(currentRow, currentCol - 1);
                }
                //else move up one row
                else {
                    MoveToPosition(currentRow - 1, currentCol);
                }
            }
            deck.RemoveAt(deck.Count - 1);
        }

        //Get description if cursor never moved
        SetDescription();
        deckCount--;
        deckText.text = "Deck: " + deckCount + "/" + MAX_DECK_SIZE;
    }

}
