using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using UnityEngine;

public class PrizePercentageCalculator : MonoBehaviour {

    public static PrizePercentageCalculator instance { get; private set; }

    void Awake() {
        instance = this;
    }

    private enum SameCardType {
        Card2, Card2_Pair2, Card2_Pair3, Card2_Pair4,
        Card3, Card3_Pair2, Card3_Pair3,
        Card4, Card4_Pair2,
        Card5,
        Card6,
        Card7,
        Card8,
        Card9,
        None
    }

    [Header("2 Cards")]
    public Combo card2;
    [Header("2 Cards, 2 Pairs")]
    public Combo card2pair2;
    [Header("2 Cards, 3 Pairs")]
    public Combo card2pair3;
    [Header("2 Cards, 4 Pairs")]
    public Combo card2pair4;
    [Header("3 Cards")]
    public Combo card3;
    [Header("3 Cards, 2 Pairs")]
    public Combo card3pair2;
    [Header("3 Cards, 3 Pairs")]
    public Combo card3pair3;
    [Header("4 Cards")]
    public Combo card4;
    [Header("4 Cards, 2 Pairs")]
    public Combo card4pair2;
    [Header("5 Cards")]
    public Combo card5;
    [Header("6 Cards")]
    public Combo card6;
    [Header("7 Cards")]
    public Combo card7;
    [Header("8 Cards")]
    public Combo card8;
    [Header("9 Cards")]
    public Combo card9;
    [Header("2 Straight")]
    public Combo straight2;
    [Header("3 Straight")]
    public Combo straight3;
    [Header("4 Straight")]
    public Combo straight4;
    [Header("5 Straight")]
    public Combo straight5;
    [Header("6 Straight")]
    public Combo straight6;
    [Header("7 Straight")]
    public Combo straight7;
    [Header("8 Straight")]
    public Combo straight8;

    public List<Prize> CalculateBonus(List<PlayedMagnus> playedMagnus) {
        //Any played invalid magnus cancels prizes for this round
        for (int i = 0; i < playedMagnus.Count; i++) {
            if (!playedMagnus[i].magnus.GetIsValid()) {
                return new List<Prize>();
            }
        }

        //Prizes only occur for more than 2 played cards
        if (playedMagnus.Count < 2) {
            return new List<Prize>();
        }

        //Straights are exclusive prizes and can't be mixed with other prizes.
        if (IsStraight(playedMagnus)) {
            List<Prize> singleStraightPrize = new List<Prize>();
            singleStraightPrize.Add(GetStraightPrize(playedMagnus.Count));
            return singleStraightPrize;
        }
        return GetSameCardPrizes(playedMagnus);
    }

    private bool IsStraight(List<PlayedMagnus> playedMagnus) {
        int straightLength = playedMagnus.Count;
        int previousValue = playedMagnus[0].spiritNumber;
        bool? increasingStraight = null;
        for (int i = 1; i < straightLength; i++) {
            int currentValue = playedMagnus[i].spiritNumber;
            if (increasingStraight == null) {
                if (Mathf.Abs(currentValue - previousValue) == 1) {
                    increasingStraight = currentValue > previousValue;
                } else {
                    return false;
                }
            } else {
                if (increasingStraight.Value) {
                    if (currentValue - previousValue != 1) {
                        return false;
                    }
                } else {
                    if (currentValue - previousValue != -1) {
                        return false;
                    }
                }
            }
            previousValue = currentValue;
        }
        return true;
    }
        
    List<Prize> GetSameCardPrizes(List<PlayedMagnus> playedMagnus) {
        // Check for same cards
        playedMagnus.Sort((a, b) => a.spiritNumber - b.spiritNumber);
        List<SameCardType> types = new List<SameCardType>();
        int streak = 1;
        int prev = playedMagnus[0].spiritNumber;
        for (int i = 1; i < playedMagnus.Count; i++) {
            int curr = playedMagnus[i].spiritNumber;
            if (curr == prev) {
                streak++;
            } else {
                // Was there no streak or is the last element different from the previous element? No bonus
                if (streak == 1 || i == playedMagnus.Count - 1) {
                    types.Clear();
                    break;
                }
                if (streak >= 2) {
                    types.Add(GetSameCardTier(streak));
                    streak = 1;
                }
            }
            if (i == playedMagnus.Count - 1 && streak >= 2) {
                types.Add(GetSameCardTier(streak));
            }
            prev = curr;
        }

        List<SameCardType> consolidatedTypes = new List<SameCardType>();
        int num2Card = (from t in types where t == SameCardType.Card2 select t).Count(); 
        int num3Card = (from t in types where t == SameCardType.Card3 select t).Count(); 
        int num4Card = (from t in types where t == SameCardType.Card4 select t).Count(); 
        types.RemoveAll(t => t == SameCardType.Card2 || t == SameCardType.Card3 || t == SameCardType.Card4);
        consolidatedTypes.AddRange(types);
        switch (num2Card) {
        case 1: consolidatedTypes.Add(SameCardType.Card2); break;
        case 2: consolidatedTypes.Add(SameCardType.Card2_Pair2); break;
        case 3: consolidatedTypes.Add(SameCardType.Card2_Pair3); break;
        case 4: consolidatedTypes.Add(SameCardType.Card2_Pair4); break;
        }
        switch (num3Card) {
        case 1: consolidatedTypes.Add(SameCardType.Card3); break;
        case 2: consolidatedTypes.Add(SameCardType.Card3_Pair2); break;
        case 3: consolidatedTypes.Add(SameCardType.Card3_Pair3); break;
        }
        switch (num4Card) {
        case 1: consolidatedTypes.Add(SameCardType.Card4); break;
        case 2: consolidatedTypes.Add(SameCardType.Card4_Pair2); break;
        }

        List<Prize> prizes = new List<Prize>();
        foreach (SameCardType type in consolidatedTypes) {
            switch (type) {
            case SameCardType.Card2:        prizes.Add(new Prize("2 Cards", card2)); break;
            case SameCardType.Card2_Pair2:  prizes.Add(new Prize("2 Cards, 2 Pairs", card2pair2)); break;
            case SameCardType.Card2_Pair3:  prizes.Add(new Prize("2 Cards, 3 Pairs", card2pair3)); break;
            case SameCardType.Card2_Pair4:  prizes.Add(new Prize("2 Cards, 4 Pairs", card2pair4)); break;
            case SameCardType.Card3:        prizes.Add(new Prize("3 Cards", card3)); break;
            case SameCardType.Card3_Pair2:  prizes.Add(new Prize("3 Cards, 2 Pairs", card3pair2)); break;
            case SameCardType.Card3_Pair3:  prizes.Add(new Prize("3 Cards, 3 Pairs", card3pair3)); break;
            case SameCardType.Card4:        prizes.Add(new Prize("4 Cards", card4)); break;
            case SameCardType.Card4_Pair2:  prizes.Add(new Prize("4 Cards, 2 Pairs", card4pair2)); break;
            case SameCardType.Card5:        prizes.Add(new Prize("5 Cards", card5)); break;
            case SameCardType.Card6:        prizes.Add(new Prize("6 Cards", card6)); break;
            case SameCardType.Card7:        prizes.Add(new Prize("7 Cards", card7)); break;
            case SameCardType.Card8:        prizes.Add(new Prize("8 Cards", card8)); break;
            case SameCardType.Card9:        prizes.Add(new Prize("9 Cards", card9)); break;
            }
        }
        return prizes;
    }

    private Prize GetStraightPrize(int size) {
        switch (size) {
        case 2: return new Prize("2 Straight", straight2);
        case 3: return new Prize("3 Straight", straight3);
        case 4: return new Prize("4 Straight", straight4);
        case 5: return new Prize("5 Straight", straight5);
        case 6: return new Prize("6 Straight", straight6);
        case 7: return new Prize("7 Straight", straight7);
        case 8: return new Prize("8 Straight", straight8);
        default: return null;
        }
    }

    private SameCardType GetSameCardTier(int num) {
        switch (num) {
        case 2: return SameCardType.Card2;
        case 3: return SameCardType.Card3;
        case 4: return SameCardType.Card4;
        case 5: return SameCardType.Card5;
        case 6: return SameCardType.Card6;
        case 7: return SameCardType.Card7;
        case 8: return SameCardType.Card8;
        case 9: return SameCardType.Card9;
        default: return SameCardType.None;
        }
    }
}
