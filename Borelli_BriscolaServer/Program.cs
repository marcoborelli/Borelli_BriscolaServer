﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Net;
using Borelli_BriscolaServer.model;

namespace Borelli_BriscolaServer {
    public class Program {
        private static List<Table> tables = new List<Table>();
        private static short Port { get; set; }

        static void Main(string[] args) {
            InitIpAndPort();
            TcpListener serverSocket = new TcpListener(IPAddress.Any, Port);
            Console.WriteLine($"Server avviato con successo sulla porta {Port}");

            TcpClient client;

            tables.Add(new Table("amanpreet", 4));
            tables.Add(new Table("prova2"));

            serverSocket.Start();


            while (true) {
                client = serverSocket.AcceptTcpClient();
                Console.WriteLine("Un utente sta provando a registrarsi");

                Task.Run(() => UserRegistration(client, false));
            }
        }

        public static void UserRegistration(TcpClient client, bool isReplay) {
            if (!isReplay)
                SendUpdateString(client);

            bool res = false;

            while (!res && !isReplay) {
                //reg:table=<id>
                string ress = ReadLineStream(client);

                if (ress == "chiudi") {
                    client.Close();
                    return;
                } else if (Regex.IsMatch(ress, @"^reg:table=(\w+)$")) {
                    string tableId = ress.Split('=')[1];

                    int tableIndex = tables.IndexOf(new Table(tableId));
                    if (tableIndex != -1) {
                        eJoinResult tmpRes = tables[tableIndex].AddPlayer(client);
                        res = (tmpRes != eJoinResult.NameExisting && tmpRes != eJoinResult.Error); //bool tenuto per retrocompatibilita' con vecchio codice

                        if (tmpRes == eJoinResult.NameExisting) {
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

            }
        }

        public static void DeleteTable(Table t) {
            tables.Remove(t);
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
            Stream stream = socket.GetStream(); //per leggere fino al \n (a volte il s.o. concatena piu' messaggi insieme che devono essere separati)
            string res = "";

            int i;
            while ((i = stream.ReadByte()) != -1) {
                char c = (char)i;
                if (c == '\n')
                    break;

                res += $"{c}";
            }

            return res;
        }

        private static void InitIpAndPort() {
            if (!File.Exists("conf")) {
                using (StreamWriter write = new StreamWriter("conf")) {
                    write.WriteLine("5000");
                }
            }

            using (StreamReader read = new StreamReader("conf")) {
                Port = short.Parse(read.ReadLine());
            }
        }
    }
}
