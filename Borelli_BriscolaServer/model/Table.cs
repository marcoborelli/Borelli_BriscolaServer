using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace Borelli_BriscolaServer.model {
    public class Table : IEquatable<Table> {
        public List<Player> Players { get; private set; }
        private List<Card> TableHand { get; set; }
        public string Id { get; private set; }

        public Table(string id, byte numMaxPlayer = 2) {
            Players = new List<Player>();
            TableHand = new List<Card>();

            Players.Capacity = numMaxPlayer;
            TableHand.Capacity = numMaxPlayer;
            Id = id;
        }

        public void AddPlayer(TcpClient socket) {
            if (Players.Count >= Players.Capacity) { //TODO rivedere (?)
                Play();
            }

            string username;
            using (StreamReader reader = new StreamReader(socket.GetStream())) {
                //reg:username=<nome>
                username = reader.ReadLine().Split(':')[1].Split('=')[1];
            }

            List<Card> pCards = new List<Card> { CardDeck.Instance.DrawCard(), CardDeck.Instance.DrawCard(), CardDeck.Instance.DrawCard() };
            using (StreamWriter writer = new StreamWriter(socket.GetStream())) {
                //reg:deck=<val1>;<val2>;<val3>
                writer.WriteLine($"reg:deck={String.Join(";", pCards)}");
            }

            Players.Add(new Player(username, socket, pCards));
        }

        public void Play() {
            byte i = 0;
            bool first = true;

            try {
                while (CardDeck.Instance.GetDeckCount() != 0) {
                    TableHand.Clear();

                    if (!first) { //pescaggio carte
                        for (byte j = i; j < i + Players.Count; j++) {
                            Card tmp = CardDeck.Instance.DrawCard();

                            using (StreamWriter writer = new StreamWriter(Players[i].ClientSocket.GetStream())) {
                                //play:cardDrawed=<val>
                                writer.WriteLine($"play:cardDrawed={tmp}");
                            }
                            Players[i].DrawCard(tmp);
                        }
                    } else
                        first = false;


                    while (i < i + Players.Count) { //gioco effettivo in cui ognuno mette giu' le carte
                        string playedCard;
                        int playerIndex = i < Players.Count ? i : i - Players.Count;

                        using (StreamReader reader = new StreamReader(Players[playerIndex].ClientSocket.GetStream())) { //per vedere la carta che e' stata giocata
                            //play:cardToPlay=<val>
                            playedCard = reader.ReadLine().Split(':')[1].Split('=')[1];
                        }
                        TableHand.Add((Card)playedCard);


                        //play:cardPlayed=<val>;player=<val>
                        SendMessageInBroadcastExceptAt(playerIndex, $"play:cardPlayed={playedCard};player={Players[playerIndex].Name}"); //si comunica anche agli altri partecipanti che la carta e' stata giocata

                        i++;
                    }

                    i = (byte)Players.IndexOf(Assess()); //in questo modo il turno alla mano dopo ripartira' da colui che ha preso per ultimo

                    //al vincitore vengono asseganti tutti i punti di quella mano
                    byte sum = 0;
                    TableHand.ForEach(x => sum += x.GetPointValue());
                    Players[i].Score += sum;
                    using (StreamWriter writer = new StreamWriter(Players[i].ClientSocket.GetStream())) {
                        //play:handWinner=<username>
                        writer.WriteLine($"play:handWinner={Players[i].Name}");
                    }

                }
            } catch (Exception e) {
                throw new Exception(e.Message);
            }


            byte puntiMax = Players.Max(x => x.Score);
            List<Player> vincitori = Players.Where(x => x.Score == puntiMax).ToList();

            SendMessageInBroadcastExceptAt(-1, $"end:winner={String.Join(";", vincitori)}");
        }


        private void SendMessageInBroadcastExceptAt(int exceptIndex, string message) { //-1 in exceptIndex vuol dire che lo si vuole mandare a tutti
            for (byte j = 0; j < Players.Count; j++) {
                if (j == exceptIndex) {
                    continue;
                }

                using (StreamWriter writer = new StreamWriter(Players[j].ClientSocket.GetStream())) {
                    writer.WriteLine(message);
                }
            }
        }

        private Player Assess() {
            if (Players.Count < 2) {
                throw new Exception("Bisogna essere almeno in due per giocare");
            }

            Player tmpWinPl = Players[0];
            Card tmpWinCr = TableHand[0];

            for (byte i = 1; i < Players.Count; i++) { //parto da 1 perche' la 0 gia' la tengo come se fosse la migliore
                tmpWinPl = AssessCouple(tmpWinPl, tmpWinCr, Players[i], TableHand[i]);

                if (Players[i].Equals(tmpWinPl)) { //Se il migliore giocatore e' uguale a quello del turno attuale e' perche' e' cambiato. Bisogna quindi cambiare anche la miglior carta
                    tmpWinCr = TableHand[i];
                }
            }

            return tmpWinPl;
        }

        private Player AssessCouple(Player p1, Card c1, Player p2, Card c2) { //valuta
            Player res = null;

            if (IsBriscola(c1) && !IsBriscola(c2)) {
                //p1.Score += (byte)(c1.GetPointValue() + c2.GetPointValue());
                res = p1;
            } else if (!IsBriscola(c1) && IsBriscola(c2)) {
                res = p2;
            } else {
                if (c1.CompareTo(c2) == -1) {
                    res = p2;
                } else if (c1.CompareTo(c2) == 1) {
                    res = p1;
                }
            }

            return res;
        }

        private static bool IsBriscola(Card c) {
            return CardDeck.Instance.Briscola.Suit == c.Suit;
        }



        public bool Equals(Table t) => t?.Id == this.Id;
    }
}
