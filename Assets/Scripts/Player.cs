using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour {

    public int maxHealth;
    private int health;

    public Text healthText;

    public int GetHealth() { return health; }

	// Use this for initialization
	void Start () {
        health = maxHealth;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void DecreaseHealth(int damage) {
        health -= damage;
        if (health < 0) {
            health = 0;
        }

        healthText.text = health + "/" + maxHealth;
    }

    public void IncreaseHealth(int hp) {
        health += hp;
        if (health > maxHealth) {
            health = maxHealth;
        }

        healthText.text = health + "/" + maxHealth;
    }
}
