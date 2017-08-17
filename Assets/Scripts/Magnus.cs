using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magnus : MonoBehaviour {

    //Magnus can be for attacking, defending, or healing. Some Magnus can be more than one type.
    private bool isAtk = false;
    private bool isDef = false;
    private bool isHeal = false;
    private bool isFinisher = false; //Ends a combo

    //Stat depends on type of magnus (atk or def). Neutral stat is used for heal Magnus.
    private int neutralStat = 0;
    private int elementStat = 0;

    //When Magnus can be played in a combo. Combo 1 means immediately, 2 means one valid magnus must be played first, etc.
    private int atkCombo = 0;
    private int defCombo = 0;

    private string magnusName = "";
    private Element element = Element.Neutral;
    private List<int> spiritNumbers = new List<int>();
    private int chosenNumberIndex = 0;
    private bool isValid = true; //choosing wrong type of magnus in atk/def phase (i.e. throwing the magnus away)

    //Getters, Setters
    public int GetSpiritNumber(int index) { return spiritNumbers[index]; }
    public int GetSpiritNumberCount() { return spiritNumbers.Count; }
    public string GetName() { return magnusName; }
    public bool GetIsAtk() { return isAtk; }
    public bool GetIsDef() { return isDef; }
    public bool GetIsHeal() { return isHeal; }
    public bool GetIsFinisher() { return isFinisher; }
    public int GetNeutralStat() { return neutralStat; }
    public int GetElementStat() { return elementStat; }
    public int GetAtkCombo() { return atkCombo; }
    public int GetDefCombo() { return defCombo; }
    public Element GetElement() { return element; }
    public bool GetIsValid() { return isValid; }
    public float GetWidth() { 
        Transform cardGraphic = transform.GetChild(0);
        float width = cardGraphic.GetComponent<SpriteRenderer>().sprite.bounds.size.x * cardGraphic.localScale.x;

        Transform ancestor = transform.parent;
        while (ancestor != null) {
            width *= ancestor.localScale.x;
            ancestor = ancestor.parent;
        }
        return width; 
    }
    public float GetHeight() {
        Transform cardGraphic = transform.GetChild(0);
        float height = cardGraphic.GetComponent<SpriteRenderer>().sprite.bounds.size.y * cardGraphic.localScale.y;

        Transform ancestor = transform.parent;
        while (ancestor != null) {
            height *= ancestor.localScale.y;
            ancestor = ancestor.parent;
        }
        return height;
    }

    public void SetName(string s) { magnusName = s; }
    public void SetIsAtk(bool b) { isAtk = b; }
    public void SetIsDef(bool b) { isDef = b; }
    public void SetIsHeal(bool b) { isHeal = b; }
    public void SetIsFinisher(bool b) { isFinisher = b; }
    public void SetNeutralStat(int i) { neutralStat = i; }
    public void SetElementStat(int i) { elementStat = i; }
    public void SetAtkCombo(int i) { atkCombo = i; }
    public void SetDefCombo(int i) { defCombo = i; }
    public void SetGraphic(Sprite graphic) { transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = graphic; }
    public void SetElement(string s) {
        switch (s) {
            case "Neutral":
                element = Element.Neutral;
                break;
            case "Water":
                element = Element.Water;
                break;
            case "Fire":
                element = Element.Fire;
                break;
            case "Light":
                element = Element.Light;
                break;
            case "Dark":
                element = Element.Dark;
                break;
            case "Wind":
                element = Element.Wind;
                break;
            case "Chronos":
                element = Element.Chronos;
                break;
            default:
                break;
        }
    }
    public void SetValid(bool b) {
        isValid = b;
        Transform cardGraphic = transform.GetChild(0);
        if (!isValid) {
            cardGraphic.GetComponent<SpriteRenderer>().color = new Color(.2f, .2f, .2f);
        }
        else {
            cardGraphic.GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

	void Start() {
        CreateSpiritNumbers();
    }

    void Update() {

    }

    //Simple implementation for spirit numbers: random 1-4 numbers with random 1-9 range.
    //Actual implementation in the game is that a specific Magnus has 4 predetermined rules for each corner.
    //The corner can be empty, an odd number, an even number, or a random range that's not necessarily 1-9.
    private void CreateSpiritNumbers() {
        int spiritNumberCount = Random.Range(1, 5);
        for (int i = 0; i < spiritNumberCount; i++) {
            int spiritNumber = Random.Range(1, 10);
            spiritNumbers.Add(spiritNumber);

            Sprite[] s = Resources.LoadAll<Sprite>("Images/spiritNumbers");
            transform.GetChild(i + 1).GetComponent<SpriteRenderer>().sprite = s[spiritNumber - 1];
        }
    }

    public void ChooseNumber(int spiritNumberIndex) {
        chosenNumberIndex = spiritNumberIndex;
        for (int i = 1; i < transform.childCount; i++) {
            if (i != chosenNumberIndex + 1) {
                transform.GetChild(i).GetComponent<SpriteRenderer>().enabled = false;
            }
        }
    }

    public void Reset() {
        chosenNumberIndex = 0;
        for (int i = 1; i < transform.childCount; i++) {
            transform.GetChild(i).GetComponent<SpriteRenderer>().enabled = true;
        }
        SetValid(true);
    }

    public void Hide() {
        for (int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    public void Show() {
        for (int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).GetComponent<SpriteRenderer>().enabled = true;
        }
    }
}
