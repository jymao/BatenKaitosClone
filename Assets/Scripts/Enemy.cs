using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour {

    public int maxHealth;
    private int health;

    public Text healthText;

    public int attack;
    public int defense;
    public int elementAttack;
    public int elementDefense;

    private int numAttacks = 0;

    //Elemental weaknesses and strengths relative to the player's elemental attacks.
    //A positive number for an affinity means the player using that element is effective against the enemy.
    //A negative number for affinity means the element is one that the enemy uses.
    //Zero means the element has no extra effect for the enemy.
    private Dictionary<Element, int> affinities = new Dictionary<Element, int>();

    public int GetHealth() { return health; }
    public int GetAttack() { return numAttacks * attack; }
    public int GetElementAttack() { return numAttacks * elementAttack; }
    public int GetDefense() { return defense; }
    public int GetElementDefense() { return elementDefense; }
    public int GetAffinity(Element element) { return affinities[element]; }

	// Use this for initialization
	void Start () {
        health = maxHealth;

        //Giacomo's second and third fight affinity values
        affinities[Element.Water] = 50;
        affinities[Element.Fire] = -50;
        affinities[Element.Light] = 30;
        affinities[Element.Dark] = -30;
        affinities[Element.Wind] = 0;
        affinities[Element.Chronos] = 0;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    //number of attacks this enemy can have a turn
    public int NumAttacks() {
        numAttacks = Random.Range(4, 8);
        return numAttacks;
    }

    public void DecreaseHealth(int damage) {
        health -= damage;
        if (health < 0) {
            health = 0;
        }

        healthText.text = health + "/" + maxHealth;
    }
}
