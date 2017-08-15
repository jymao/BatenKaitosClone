using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using UnityEngine;

public class Prize {
    public string name { get; private set; }
    private Combo combo;
    public double offense { get { return combo.offense; } }
    public double defense { get { return combo.defense; } }

    public Prize(string name, Combo combo) {
        this.name = name;
        this.combo = combo;
    }
}
