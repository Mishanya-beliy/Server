using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static Server.Client;

namespace Server
{
    class Server
    {
        internal static TcpListener serverTCP;
        internal static UdpClient serverUDP;
        public static int Port { get; private set; }
        public static Dictionary<int, Client> clients;
        public static Dictionary<int, Room> rooms;


        public static void Start(int port)
        {
            Port = port;
            try
            {
                rooms = new Dictionary<int, Room>();
                clients = new Dictionary<int, Client>();

                serverTCP = new TcpListener(IPAddress.Any, Port);
                serverTCP.Start();
                rooms.Add(-1, new Room(-1, "Queue", "3228"));
                Console.Title = $"======================================Server [{serverTCP.LocalEndpoint}] working=======================================";
                Logger("[Server]Start");

                ConnectingTCP();
                serverUDP = new UdpClient(Port);
                serverUDP.Client.SendBufferSize = 512;
                serverUDP.Client.SendBufferSize = 1024;
                ConnectingUDP();
            }
            catch (SocketException e)
            {
                Logger("SocketException: " + e);
                Logger("Reboot server");
                StopServer();
            }
        }

        private static async void ConnectingTCP()
        {
            try
            {
                Logger("[TCPServer]Ready for connections...");
                TcpClient client = await serverTCP.AcceptTcpClientAsync();
                Logger($"[Client {client.Client.RemoteEndPoint}] connected by TCP!");

                ConnectingTCP();
                
                int id;
                while (!clients.TryAdd(id = CreateUniqueId(clients), new Client(id, -1, client))) ;
                rooms.TryGetValue(-1, out Room room);
                room.players.Add(id, new Player());
                Send.Welcome(id);
            }
            catch (ObjectDisposedException e)
            {
                Logger($"[TCPServer]Error accept client: {e.Message}.");
            }
        }        
        private static async void ConnectingUDP()
        {
            try
            {
                UdpReceiveResult result = await serverUDP.ReceiveAsync();

                if (result.Buffer.Length > 0)
                {

                    using Packet packet = new Packet(result.Buffer);

                    int clientId = packet.ReadInt();
                    packet.RemoveInt();

                    if (clientId > -1)
                    {

                        UDP clientUDP = clients[clientId].udp;
                        if (clientUDP.endPoint == null)
                            if (clients[clientId].Authorized) clientUDP.Connect(result.RemoteEndPoint);
                            else return;

                        if (clientUDP.endPoint.ToString() == result.RemoteEndPoint.ToString())
                            clientUDP.HandlePacket(packet);
                    }
                }

                ConnectingUDP();
            }
            catch (Exception e) {
                Logger($"[UDPServer]Error accept client: {e.Message}."); }
        }

        internal static async void SendUDP(IPEndPoint endPoint, Packet packet)
        {
            try {
                if (endPoint != null) await serverUDP.SendAsync(packet.ToArray(), packet.Length(), endPoint); }
            catch (Exception ex) {
                Console.WriteLine($"[UDPServer:{endPoint}]Error sending data via UDP: {ex}"); }
        }

        public static void CreateRoom(int idPlayer, string name, string password)
        {
            if (clients.TryGetValue(idPlayer, out Client client))
            {
                int id;
                Room room;
                while (!rooms.TryAdd(id = CreateUniqueId(rooms), room = new Room(id, name, password))) ;
                room.Enter(rooms[client.IdRoom].Exit(idPlayer), idPlayer);
            }
        }

        internal static void FindPrivateRoom(int idPlayer, string name, string password)
        {
            int.TryParse(name, out int idRoom);
            if (rooms.TryGetValue(idRoom, out Room room))
            {
                if (!room.isPublic)
                {
                    if (room.password == password)
                    {
                        if (clients.TryGetValue(idPlayer, out Client client))
                            room.Enter(rooms[client.IdRoom].Exit(idPlayer), idPlayer);
                    }
                    else Send.ErrorFindPrivateRoom(idPlayer, "Incorrect password");
                }
                else Send.ErrorFindPrivateRoom(idPlayer, "This room is not private");
            }
            else Send.ErrorFindPrivateRoom(idPlayer, "Room not found");
        }

        public static void StopServer()
        {
            Console.Title = $"==============================================Server stop================================================";
            serverTCP.Stop();
            Console.Title = $"============================================Server rebooting=============================================";
            Start(Port);
        }

        //Log info
        public static int LogMessageMaxCount { get; private set; } = 25;
        public static List<string> logMessage = new List<string>();

        public static void Logger(string message)
        {
            DateTime now = DateTime.Now;
            logMessage.Add($"[{now.Hour}:{now.Minute}:{now.Second}:{now.Millisecond}]{message}");
            if (logMessage.Count > LogMessageMaxCount)
                logMessage.RemoveAt(0);
        }

        private static int CreateUniqueId<TValue>(Dictionary<int, TValue> list)
        {
            int id = 0;
            while (true)
            {
                if (!list.ContainsKey(id)) return id;
                id++;
            }
        }
    }
}