using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magnus : MonoBehaviour {
	public Element element { get; private set; }
	public int neutralAtk { get; private set; }
	public int elementAtk { get; private set; }
	public List<int> spiritNumbers { get; private set; }

	public Magnus(Element element, int neutralAtk, int elementAtk, List<int> spiritNumbers) {
		this.element = element;
		this.neutralAtk = neutralAtk;
		this.elementAtk = elementAtk;
		this.spiritNumbers = spiritNumbers;
	}
}
