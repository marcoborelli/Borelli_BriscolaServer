using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Borelli_BriscolaServer.model {
    public class Player : IEquatable<Player> {
        public string Name { get; private set; }
        public List<Card> Hand { get; private set; }
        public byte Score { get; internal set; }
        public TcpClient ClientSocket { get; set; }

        public Player(string name, TcpClient socket) {
            Name = name;
            Score = 0;

            Hand = new List<Card>();
            Hand.Capacity = 3;

            ClientSocket = socket;
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

            //play:cardDrawed=<val>
            Program.WriteLineStream(ClientSocket, $"play:cardDrawed={c}");
            Hand.Add(c);
        }



        public bool Equals(Player p) {
            if (p == null) {
                return false;
            } else if (p == this) {
                return true;
            } else
                return p.ClientSocket.Equals(this.ClientSocket);
        }

        public override string ToString() {
            return $"{Name}";
        }
    }
}
