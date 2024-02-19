using System;

namespace Borelli_BriscolaServer.model {
    public enum eValue {
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Knave, //fante
        Knight, //cavallo
        King, //re
        Ace //asso
    }

    public enum eSuits {
        Coins, //oro
        Swords, //spade
        Cups, //coppe
        Batons //bastoni
    }

    public class Card : IComparable<Card>, IEquatable<Card> {
        public eValue Value { get; private set; }
        public eSuits Suit { get; private set; }

        public Card(eValue val, eSuits suit) {
            Value = val;
            Suit = suit;
        }

        public byte GetPointValue() {
            byte toRet;

            switch (Value) {
                case eValue.Ace:
                    toRet = 11;
                    break;
                case eValue.Three:
                    toRet = 10;
                    break;
                case eValue.King:
                    toRet = 4;
                    break;
                case eValue.Knight:
                    toRet = 3;
                    break;
                case eValue.Knave:
                    toRet = 2;
                    break;
                default:
                    toRet = 0;
                    break;
            }

            return toRet;
        }


        public static explicit operator Card(string s) {
            string[] fields = s.Split('_');
            return new Card((eValue)Enum.Parse(typeof(eValue), fields[0]), (eSuits)Enum.Parse(typeof(eSuits), fields[1]));
        }
        //TODO: mettere gia' qui il controllo se sia briscola (?)
        public int CompareTo(Card c) { //1 this > c; -1 c > this
            if (Suit == c.Suit) { //se il seme non e' lo stesso (senza contare le briscole) vince sempre il primo che ha messo giu'
                return 1;
            }

            if (GetPointValue() < c.GetPointValue() || (byte)Value < (byte)c.Value) {
                return -1;
            } else if (GetPointValue() > c.GetPointValue() || (byte)Value > (byte)c.Value) {
                return 1;
            } else {
                return 0;
            }
        }

        public bool Equals(Card c) {
            if (c == null) {
                return false;
            } else if (c == this) {
                return true;
            } else {
                return (c.Suit == Suit && c.Value == Value);
            }
        }

        public override string ToString() {
            return $"{Value}_{Suit}";
        }
    }
}
