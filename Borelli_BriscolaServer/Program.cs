using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using Borelli_BriscolaServer.model;

namespace Borelli_BriscolaServer {
    public class Program {
        private static List<Table> tables = new List<Table>();
        static void Main(string[] args) {
            TcpListener serverSocket = new TcpListener(IPAddress.Any, 5000);
            Console.WriteLine("Server avviato con successo!");

            TcpClient client;

            tables.Add(new Table("amanpreet", 4));

            serverSocket.Start();


            while (true) {
                client = serverSocket.AcceptTcpClient();
                Console.WriteLine("Un utente sta provando a registrarsi");

                Task.Run(() => UserRegistration(client));
            }
            /*for (int i = 0; i < 40; i++) {
                Console.WriteLine($"'{CardDeck.Instance.DrawCard()}'");
            }

            Console.WriteLine($"Briscola è: '{CardDeck.Instance.Briscola}'");

            Console.ReadKey();*/
        }

        private static void UserRegistration(TcpClient client) {
            SendUpdateString(client);

            bool res = false;

            do {
                //reg:table=<id>
                string ress = ReadLineStream(client);

                if (Regex.IsMatch(ress, @"^chiudi$")) {
                    client.Close();
                } else if (Regex.IsMatch(ress, @"^reg:table=(\w+)$")) {
                    string tableId = ress.Split('=')[1];

                    int tableIndex = tables.IndexOf(new Table(tableId));
                    if (tableIndex != -1) {
                        res = tables[tableIndex].AddPlayer(client);

                        if (!res) {
                            Program.WriteLineStream(client, "reg:addUserRes=error");
                        }
                    }
                } else if (Regex.IsMatch(ress, @"^reg:createTable=(\w+);numPart=([0-9]+)$")) {
                    Console.WriteLine("Un utente sta provando a creare una nuova stanza");

                    string tableName = ress.Split('=')[1].Split(';')[0];
                    byte numPart = byte.Parse(ress.Split('=')[2]);

                    if (tables.Contains((Table)tableName) || (numPart != 2 && numPart != 4)) {
                        Console.WriteLine($"Errore nel tentare di creare la stanza '{tableName}'");
                        Program.WriteLineStream(client, "reg:addTableRes=error");
                        continue;
                    }

                    tables.Add(new Table(tableName, numPart));
                    Program.WriteLineStream(client, "reg:addTableRes=ok");
                    Console.WriteLine($"È stata creata la stanza '{tableName}'");
                } else if (ress == "preReg:update") {
                    SendUpdateString(client);
                }

            } while (!res);
        }

        private static void SendUpdateString(TcpClient client) {
            //preReg:<id,MaxPart,Part>;...
            WriteLineStream(client, $"preReg:{String.Join(";", tables)};");
        }

        public static void WriteLineStream(TcpClient socket, string toWrite) {
            byte[] bytes = Encoding.ASCII.GetBytes($"{toWrite}\n");

            socket.GetStream().Write(bytes, 0, bytes.Length);
            socket.GetStream().Flush();
        }

        public static string ReadLineStream(TcpClient socket) {
            byte[] bytes = new byte[socket.ReceiveBufferSize];
            int numBytes = socket.GetStream().Read(bytes, 0, socket.ReceiveBufferSize);

            return Encoding.ASCII.GetString(bytes, 0, numBytes).Trim();
        }
    }
}
