using System;
using System.Collections.Generic;

namespace Borelli_BriscolaServer.model {
    public class CardDeck {
        private static CardDeck _instance;
        private List<Card> _deck;
        public Card Briscola { get; private set; }


        public static CardDeck Instance {
            get {
                if (_instance == null) {
                    _instance = new CardDeck();
                }
                return _instance;
            }
        }



        private CardDeck() {
            _deck = new List<Card>();

            CreateRandomDeck();
            Briscola = _deck[0];
        }



        public Card DrawCard() {
            if (_deck.Count > 0) {
                Card toRet = _deck[_deck.Count - 1];
                _deck.Remove(toRet);

                return toRet;
            }

            throw new Exception("Il mazzo di carte e' gia' vuoto");
        }



        private void CreateRandomDeck() {
            for (byte i = 0; i < 10; i++) {
                for (byte j = 0; j < 4; j++) {
                    _deck.Add(new Card((eValue)i, (eSuits)j));
                }
            }

            RandomizeDeck();
        }

        private void RandomizeDeck() {
            Random _rand = new Random();

            for (int i = _deck.Count - 1; i > 0; i--) {
                var k = _rand.Next(i + 1);
                var value = _deck[k];
                _deck[k] = _deck[i];
                _deck[i] = value;
            }
        }
    }
}
