using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Combo definition where offense and defense values represent
/// damage multipliers
/// </summary>
[System.Serializable]
public class Combo {
    public double offense = 1.0;
    public double defense = 1.0;
}