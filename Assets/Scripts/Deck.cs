using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour {

    private List<GameObject> deck = new List<GameObject>();
    private List<GameObject> graveyard = new List<GameObject>(); //previously played cards

    public int GetCount() { return deck.Count; }

    void Awake() {
        DontDestroyOnLoad(gameObject); //keep deck from deck editor to game scene
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    //Shuffle Magnus deck using the Fisher-Yates shuffle algorithm
    public void ShuffleDeck() {
        int count = deck.Count;
        while (count > 1) {
            count--;
            int k = UnityEngine.Random.Range(0, count + 1);
            GameObject temp = deck[k];
            deck[k] = deck[count];
            deck[count] = temp;
        }
    }

    public void ReInitDeck() {
        while (graveyard.Count > 0) {
            deck.Add(graveyard[0]);
            graveyard.RemoveAt(0);
        }
    }

    public GameObject DrawCard() {
        GameObject card = deck[0];
        deck.RemoveAt(0);
        return card;
    }

    public void AddToGraveyard(GameObject card) {
        graveyard.Add(card);
    }

    //Used by deck editor to initialize deck before sending to game scene
    public void AddToDeck(GameObject card) {
        deck.Add(card);
        //make card a child of deck so cards don't get deleted when loading game scene
        card.transform.SetParent(transform);
        card.transform.position = transform.position;
        //Some cards are hidden due to being in a non-visible row in the deck editor
        card.GetComponent<Magnus>().Show();
    }
}
