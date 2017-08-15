using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a magnus that has been selected from the hand and played
/// </summary>
public class PlayedMagnus {

    public Magnus magnus { get; private set; }
    public int spiritNumber { get; private set; }

    public PlayedMagnus(Magnus magnus, int spiritNumber) {
        this.magnus = magnus;
        this.spiritNumber = spiritNumber;
    }
}
