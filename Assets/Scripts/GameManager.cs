using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : MonoBehaviour {

    private bool isGameOver;
    private bool isPlayerTurn;

    private string[] magnusList;

    public GameObject magnusPrefab;

    // Use this for initialization
    void Start() {
        TextAsset magnusListAsset = Resources.Load<TextAsset>("MagnusList");

        string[] newline = { Environment.NewLine };
        magnusList = magnusListAsset.text.Split(newline, StringSplitOptions.RemoveEmptyEntries);

        CreateMagnus("Beer");
    }

    // Update is called once per frame
    void Update() {

    }

    private void CreateMagnus(string name) {
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
            }
        }
    }

    private string GetPropertyValue(string line) {
        char[] space = { ' ' };
        string[] words = line.Split(space, StringSplitOptions.RemoveEmptyEntries);
        return words[2];
    }
}
