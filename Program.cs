using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        public static void Main(string[] args)
        {
            Server.Start(420);
            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);
            Task.Run(() => WriteCommand());
            do
            {
                List<List<List<string>>> tables = new List<List<List<string>>>
                {
                    new List<List<string>>()
                };

                tables[0].Add(new List<string>());
                tables[0].Add(new List<string>());
                tables[0].Add(new List<string>());
                Room room;
                for (int i = 0; i < Server.rooms.Count; i++)
                {                    
                    room = Server.rooms.ElementAt(i).Value;
                    tables[0][0].Add(room.id.ToString());
                    tables[0][1].Add(room.name);
                    tables[0][2].Add(room.players.Count.ToString());
                }

                tables.Add(new List<List<string>>());
                tables[1].Add(new List<string>());
                tables[1].Add(new List<string>());
                tables[1].Add(new List<string>());
                tables[1].Add(new List<string>());
                Client client;
                for (int i = 0; i < Server.clients.Count; i++)
                {
                    try
                    {
                        client = Server.clients.ElementAt(i).Value;
                        if (client.tcp.client != null)
                        {
                            tables[1][0].Add(client.Id.ToString());
                            tables[1][1].Add(client.Name);
                            tables[1][2].Add(client.Authorized.ToString());
                            tables[1][3].Add(client.tcp.client.Client.RemoteEndPoint.ToString());
                        }
                    }
                    catch (NullReferenceException ex)
                    {
                        Server.Logger(ex.ToString());
                    }
                }
                Console.Clear();

                Console.WriteLine(value: $"{SeparatorUP(0)} {SeparatorUP(1)}");
                Console.WriteLine(Write("ID", "Name room", "Players") + " " + Write("ID", "Nick name", "Auth", "IP:Port"));

                //=============count table in line
                int tablesOnLine = 2;
                for (int loop = 0; loop < tables.Count / tablesOnLine; loop++)
                {
                    for (int line = 0; line < tables[loop / tablesOnLine][0].Count + 1 || line < tables[loop / tablesOnLine + 1][0].Count + 1; line++)
                    {
                        string s = "";
                        string separators = "";
                        if (line < tables[loop / tablesOnLine][0].Count)
                        {
                            separators += SeparatorMID(tables[loop / tablesOnLine].Count - 3);
                            s = Write(tables[loop / tablesOnLine][0][line], tables[loop / tablesOnLine][1][line], tables[loop / tablesOnLine][2][line]);
                        }
                        else if (line == tables[loop / tablesOnLine][0].Count)
                        {
                            separators = SeparatorDOWN(tables[loop / tablesOnLine].Count - 3);
                            s = $"{" ",40}";
                        }
                        else
                        {
                            separators = $"{" ",40}";
                            s = $"{" ",40}";
                        }

                        if (line < tables[loop / tablesOnLine + 1][0].Count)
                        {
                            s += " " + Write(tables[loop / tablesOnLine + 1][0][line], tables[loop / tablesOnLine + 1][1][line],
                                                tables[loop / tablesOnLine + 1][2][line], tables[loop / tablesOnLine + 1][3][line]);
                            separators += " " + SeparatorMID(tables[loop / tablesOnLine + 1].Count - 3);
                        }
                        else if(line == tables[loop / tablesOnLine + 1][0].Count)
                            separators += " " + SeparatorDOWN(tables[loop / tablesOnLine + 1].Count - 3);

                        Console.WriteLine(separators);
                        Console.WriteLine(s);
                         
                    }
                }

                if (Server.serverTCP.Pending()) Console.Write("   V");
                else Console.Write("   X");
                Console.Write(" - TCP Server status.");
                
                if (Server.serverUDP.Client.Connected) Console.Write("   V");
                else Console.Write("   X");
                Console.WriteLine(" - UDP Server status.");

                foreach(Client tmpClient in Server.clients.Values)
                Console.WriteLine($"   [Player:{tmpClient.Id}]Sync package per second:{tmpClient.syncPackagePerSecond}. All package count:{tmpClient.countSyncPackage}");

                for (int i = 0; i < Server.logMessage.Count; i++)
                    Console.WriteLine(Server.logMessage[i]);
                Thread.Sleep(3000);
            } while (true);
        }
        private static void WriteCommand()
        {
            while (true)
                if (Console.ReadLine() == "dis") Server.clients[0].Disconnect();
        }
        private static string Write(string id, string name, string players)
        {
            return $"\x2551{id,4} \x2551{name,21} \x2551{players,8} \x2551";
        }
        private static string Write(string id, string name, string players, string four)
        {
            return $"\x2551{id,4} \x2551{name,21} \x2551{players,8} \x2551{four,22} \x2551";
        }

        static string separatorUp = "\x2554═════\x2566══════════════════════\x2566═════════";
        static string separatorMidle = "\x2560═════\x256C══════════════════════\x256C═════════";
        static string separatorDown = "\x255A═════\x2569══════════════════════\x2569═════════";
        static string separatorAdd = "═══════════════════════";
        private static string SeparatorUP(int howAdd)
        {
            return Separator(separatorUp, "\x2557", howAdd);
        }
        private static string SeparatorMID(int howAdd)
        {
            return Separator(separatorMidle, "\x2563", howAdd);
        }
        private static string SeparatorDOWN(int howAdd)
        {
            return Separator(separatorDown, "\x255D", howAdd);
        }
        private static string Separator(string separator, string end, int howAdd)
        {
            string line = separator;
            for (int i = 0; i < howAdd; i++)
            {
                if (separator == separatorUp) line += "\x2566";
                else if (separator == separatorMidle) line += "\x256C";
                else line += "\x2569";
                line += separatorAdd;
            }
            
            return line += end;
        }

        [System.Runtime.InteropServices.DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            foreach (Client client in Server.clients.Values)
                client.Disconnect();
            return true;
        }
    }
}