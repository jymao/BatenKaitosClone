using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {

    public int maxHealth;
    private int health;

    public int attack;
    public int elementAttack;
    private List<Element> affinities = new List<Element>();


	// Use this for initialization
	void Start () {
        affinities.Add(Element.Fire);
        affinities.Add(Element.Dark);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    //number of attacks this enemy can have a turn
    public int NumAttacks() {
        return Random.Range(4, 8);
    }
}
