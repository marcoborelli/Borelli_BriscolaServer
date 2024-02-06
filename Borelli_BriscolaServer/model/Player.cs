using System;
using System.Collections.Generic;

namespace Borelli_BriscolaServer.model {
    public class Player {
        public string Name { get; private set; }
        public List<Card> Hand { get; private set; }
        public byte Score { get; internal set; }

        public Player(string name, Card[] initHand) {
            Name = name;
            Score = 0;

            if (initHand.Length != 3) {
                throw new Exception("Carte inziali non valide");
            }
            Hand = new List<Card>() { initHand[0], initHand[1], initHand[2] };
        }

        public Card PlayCard(byte index) {
            if (Hand.Count == 0) {
                throw new Exception("Impossibile giocare una carta");
            }
            if (index < 0 || index > 2) {
                throw new Exception("Inserire un indice di carta valida");
            }

            Card toRet = Hand[index];
            Hand.Remove(toRet);

            return toRet;
        }

        public void DrawCard(Card c) {
            if (c == null) {
                throw new Exception("La carta e' nulla");
            }
            if (Hand.Count > 2) {
                throw new Exception("Non si possono pescare carte, se ne hanno gia' minimo 3 in mano");
            }

            Hand.Add(c);
        }
    }
}
