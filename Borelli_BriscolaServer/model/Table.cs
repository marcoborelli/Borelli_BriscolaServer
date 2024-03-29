﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;

namespace Borelli_BriscolaServer.model {

    public enum eJoinResult {
        Success,
        NameExisting,
        Error
    }

    public class Table : IEquatable<Table> {
        public List<Player> Players { get; private set; }
        private List<Card> TableHand { get; set; }
        private CardDeck Deck { get; set; }
        public string Id { get; private set; }

        public Table(string id, byte numMaxPlayer = 2) {
            Players = new List<Player>();
            TableHand = new List<Card>();

            Deck = new CardDeck();

            Players.Capacity = numMaxPlayer;
            TableHand.Capacity = numMaxPlayer;
            Id = id;
        }

        public eJoinResult AddPlayer(TcpClient socket) {
            //reg:username=<nome>
            string username = Program.ReadLineStream(socket).Split('=')[1];

            //reg:res=<ok|error>
            if (Players.Exists(u => u.Name == username)) {
                return eJoinResult.NameExisting;
            }
            Players.Add(new Player(username, socket));

            try {
                if (Players.Count != Players.Capacity) {
                    //reg:update=<username>;...
                    SendMessageInBroadcastExceptAt(-1, $"reg:update={String.Join(";", Players)};"); //si aggiornano anche gli altri giocatori che se ne e' unito uno nuovo
                } else {
                    SendMessageInBroadcastExceptAt(-1, "reg:state=start");
                    Task.Run(Play);
                }
            } catch {
                for (byte e = 0; e < Players.Count; e++) {
                    try {
                        TcpClient clientTmp = Players[e].ClientSocket;
                        Program.WriteLineStream(clientTmp, "reg:error");

                        if (e != Players.Count - 1) //l'ultimo che e' arrivato e' ancora nel suo thread originale, senno' ne farei partire due che si darebbero fastidio
                            Task.Run(() => Program.UserRegistration(clientTmp, false));
                    } catch {
                        continue;
                    }
                }

                Players.Clear();
                return eJoinResult.Error;
            }

            return eJoinResult.Success;
        }

        public void Play() {
            byte i = (byte)(new Random().Next(0, Players.Count));
            bool first = true;

            try {
                SendMessageInBroadcastExceptAt(-1, $"play:players={String.Join(";", Players)};");

                for (byte q = 0; q < 3; q++) { //distribuzione carte iniziali
                    for (byte j = 0; j < Players.Count; j++) {
                        Players[j].DrawCard(Deck.DrawCard());
                    }
                }

                SendMessageInBroadcastExceptAt(-1, $"play:briscola={Deck.Briscola}");


                while (Deck.GetDeckCount() != 0 || Players[0].Hand.Count != 0) {
                    TableHand.Clear();

                    if (!first && Deck.GetDeckCount() != 0) { //pescaggio carte
                        for (byte j = i; j < i + Players.Count; j++) {
                            int playerIndex = j < Players.Count ? j : j - Players.Count;
                            Players[playerIndex].DrawCard(Deck.DrawCard());
                        }
                    } else
                        first = false;

                    SendMessageInBroadcastExceptAt(-1, $"play:cardRemaingNum={Deck.GetDeckCount()}");

                    byte origCount = (byte)(i + Players.Count);
                    while (i < origCount) { //gioco effettivo in cui ognuno mette giu' le carte
                        int playerIndex = i < Players.Count ? i : i - Players.Count;

                        SendMessageInBroadcastExceptAt(-1, $"play:turn={Players[playerIndex].Name}");

                        //play:cardToPlay=<val>
                        string playedCard = Program.ReadLineStream(Players[playerIndex].ClientSocket).Split('=')[1];

                        //il giocatore gioca la carta
                        byte cardIndex = (byte)Players[playerIndex].Hand.IndexOf((Card)playedCard);
                        TableHand.Add(Players[playerIndex].PlayCard(cardIndex));


                        //play:cardPlayed=<val>;player=<val>
                        SendMessageInBroadcastExceptAt(playerIndex, $"play:cardPlayed={playedCard};player={Players[playerIndex].Name}"); //si comunica anche agli altri partecipanti che la carta e' stata giocata

                        i++;
                    }

                    Thread.Sleep(3000);

                    i = (byte)Players.IndexOf(Assess((byte)(origCount - Players.Count))); //in questo modo il turno alla mano dopo ripartira' da colui che ha preso per ultimo

                    //al vincitore vengono asseganti tutti i punti di quella mano
                    byte sum = 0;
                    TableHand.ForEach(x => sum += x.GetPointValue());
                    Players[i].Score += sum;
                    Program.WriteLineStream(Players[i].ClientSocket, $"play:points={Players[i].Score}");

                    //play:handWinner=<username>
                    SendMessageInBroadcastExceptAt(-1, $"play:handWinner={Players[i].Name}");
                }
            } catch {
                for (byte e = 0; e < Players.Count; e++) {
                    try {
                        TcpClient clientTmp = Players[e].ClientSocket; //senno' potrei resettare la lista prima che il task sia effettivamente partito
                        Program.WriteLineStream(clientTmp, "play:error");
                        Task.Run(() => Program.UserRegistration(clientTmp, false));
                    } catch {
                        continue;
                    }
                }

                Players.Clear();
                ResetValues();

                return;
            }


            byte puntiMax = Players.Max(x => x.Score);
            List<Player> vincitori = Players.Where(x => x.Score == puntiMax).ToList();

            SendMessageInBroadcastExceptAt(-1, $"end:winner={String.Join(";", vincitori)};");

            ResetValues();
            for (sbyte p = 0; p < Players.Count; p++) {
                Player tmp = Players[p];
                bool res = PlayAgain(tmp);

                if (!res) {
                    Players.RemoveAt(p--);
                } else {
                    tmp.ResetValues();
                }

                Program.WriteLineStream(tmp.ClientSocket, $"end:closeForm");
                Task.Run(() => Program.UserRegistration(tmp.ClientSocket, res)); //viene rimandato alla pagina
            }

            if (Players.Count != 0) {
                SendMessageInBroadcastExceptAt(-1, "reg:state=start");
                Task.Run(Play);
            } else {
                Program.DeleteTable(this);
            }
        }


        private void SendMessageInBroadcastExceptAt(int exceptIndex, string message) { //-1 in exceptIndex vuol dire che lo si vuole mandare a tutti
            for (byte j = 0; j < Players.Count; j++) {
                if (j == exceptIndex) {
                    continue;
                }

                Program.WriteLineStream(Players[j].ClientSocket, message);
            }
        }

        private Player Assess(byte baseIndex) {
            if (Players.Count < 2) {
                throw new Exception("Bisogna essere almeno in due per giocare");
            }

            byte cardIndex = 0; //il tavolo ha come 0 il primo che gioca quindi si parte sempre da 0

            Player tmpWinPl = Players[baseIndex];
            Card tmpWinCr = TableHand[cardIndex];

            baseIndex++; //parto da 1 in piu' perche' il primo gia' lo tengo come se fosse il migliore
            cardIndex++;

            for (byte i = baseIndex; i < (baseIndex - 1) + Players.Count; i++, cardIndex++) { //nella condizione tolgo 1 perche' mi serve il dato 'originale'
                byte playerIndex = i < Players.Count ? i : (byte)(i - Players.Count);

                tmpWinPl = AssessCouple(tmpWinPl, tmpWinCr, Players[playerIndex], TableHand[cardIndex]);

                if (Players[playerIndex].Equals(tmpWinPl)) { //Se il migliore giocatore e' uguale a quello del turno attuale e' perche' e' cambiato. Bisogna quindi cambiare anche la miglior carta
                    tmpWinCr = TableHand[cardIndex];
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
                if (c1.Suit != c2.Suit) {
                    res = p1;
                } else {
                    if (c1.CompareTo(c2) == -1) {
                        res = p2;
                    } else if (c1.CompareTo(c2) == 1) {
                        res = p1;
                    }
                }
            }

            return res;
        }

        private bool IsBriscola(Card c) {
            return Deck.Briscola.Suit == c.Suit;
        }

        private void ResetValues() {
            TableHand.Clear();
            Deck = new CardDeck();
        }

        private bool PlayAgain(Player p) {
            string readed = Program.ReadLineStream(p.ClientSocket);
            return bool.Parse(readed.Split('=')[1]);
        }


        public static explicit operator Table(string s) {
            return new Table(s);
        }

        public bool Equals(Table t) => t?.Id == this.Id;

        public override string ToString() {
            return $"{Id},{Players.Capacity},{Players.Count}";
        }
    }
}
