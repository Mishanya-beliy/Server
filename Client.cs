using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    class Client
    {
        internal TCP tcp;
        internal UDP udp;
        public static readonly int dataBufferSize = 4096;

        public int Id { get; private set; }
        internal int IdRoom { get; private set; } = -2;

        public string Name { get; private set; } = "unnammed";
        public bool Authorized { get; private set; }

        internal EndPoint disconnectPoint { get; private set; }
        private DateTime disconnectTime;

        internal int countSyncPackage = 0;
        internal int syncPackagePerSecond = 0;
        private int syncPackagePerNowSecond = 0;
        internal DateTime lactSecond = DateTime.Now;
        internal void AddSyncPackage()
        {
            countSyncPackage++;
            if (lactSecond.Second == DateTime.Now.Second)
                syncPackagePerNowSecond++;
            else
            {
                syncPackagePerSecond = syncPackagePerNowSecond;
                syncPackagePerNowSecond = 0;
                lactSecond = DateTime.Now;
            }
        }


        public Client(int id, int idRoom, TcpClient socket)
        {
            Id = id;
            IdRoom = idRoom;

            InitializeClientData();
            tcp = new TCP(id, socket, this);
            udp = new UDP(id);
        }

        internal void Reconnect(string disconnectPoint, int idClient)
        {
            if(disconnectPoint != null)
                if(disconnectPoint.ToString() == disconnectPoint)
                {
                    tcp = new TCP(Id, Server.clients[idClient].tcp.client, this);

                    disconnectTime = new DateTime();
                    Server.rooms[Server.clients[idClient].IdRoom].players.Remove(idClient);
                    Server.clients.Remove(idClient);
                }
        }
        public void Disconnect()
        {
            disconnectTime = DateTime.Now;

            tcp.Disconnect();
            udp.Disconnect();
            Thread.Sleep(30000);
            RemoveClient();
        }

        private void RemoveClient()
        {
            if (disconnectTime != new DateTime())
                if (DateTime.Now - disconnectTime >= TimeSpan.FromSeconds(29))
                    if (tcp.client == null)
                    {
                        Server.clients.Remove(Id);
                        Server.rooms[IdRoom].players.Remove(Id);
                    }
                    else disconnectTime = new DateTime();
                else
                {
                    Thread.Sleep(30000);
                    RemoveClient();
                }
        }

        internal void Authorizated(string name)
        {
            Name = name;
            Authorized = true;
        }

        private const string successfulConnectToRoom = "Successful connect to room.";
        public void ChangeRoom(int idRoom)
        {
            IdRoom = idRoom;

            Send.ConnectToRoom(Id, successfulConnectToRoom);
            Room rooom = Server.rooms[IdRoom];
            Send.RoomInfo(Id, rooom.id, rooom.name, rooom.players.Count);
        }
        
        public class TCP
        {
            private readonly int id;
            
            public TcpClient client;
            private NetworkStream stream;
            private Packet recivedPacket;
            private Client thisClient;

            public TCP(int id, TcpClient socket, Client Client)
            {
                this.id = id;
                thisClient = Client;

                Connect(socket);
            }
            private void Connect(TcpClient socket)
            {
                client = socket;

                recivedPacket = new Packet();
                socket.SendBufferSize = dataBufferSize;
                socket.ReceiveBufferSize = dataBufferSize;

                stream = socket.GetStream();
                Read(stream);
            }
            private async void Read(NetworkStream stream)
            {
                try
                {
                    byte[] data = new byte[64];
                    int length = await stream.ReadAsync(data, 0, 64);// System.StackOverflowException
                    if (length <= 0)
                    {
                        Server.clients[id].Disconnect();
                        return;
                    }

                    byte[] packet = new byte[length];
                    Array.Copy(data, packet, length);

                    recivedPacket.Reset(HandleRecivedData(packet));
                    Read(stream);
                }
                catch (IOException ex)
                {
                    Server.Logger($"[TCPServer:{id}]IOException: {ex}.\nError code: {ex.HResult}");//-2146232800
                    Server.clients[id].Disconnect();
                }
                catch (Exception ex) 
                {
                    Server.Logger($"[TCPServer:{id}]Can`t read: {ex}.\nError code: {ex.HResult}");
                    Server.clients[id].Disconnect();
                }
            }
            private bool HandleRecivedData(byte[] data)
            {
                int packetLenght = 0;

                recivedPacket.SetBytes(data);

                if (recivedPacket.UnreadLength() >= 4)
                {
                    packetLenght = recivedPacket.ReadInt();
                    if (packetLenght <= 0) return true;
                }

                while (packetLenght > 0 && packetLenght <= recivedPacket.UnreadLength())
                {
                    byte[] packetBytes = recivedPacket.ReadBytes(packetLenght);

                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetId = packet.ReadInt();
                        if (packetId != 1 && packetId != 2 && !Server.clients[id].Authorized)
                            return true;
                        packetHandlers[packetId](id, packet);
                    }

                    packetLenght = 0;
                    if (recivedPacket.UnreadLength() >= 4)
                    {
                        packetLenght = recivedPacket.ReadInt();
                        if (packetLenght <= 0) return true;
                    }
                }
                if (packetLenght <= 1) return true;

                return false;
            }
            public async void Write(Packet packet)
            {
                try {
                    if (client != null) await stream.WriteAsync(packet.ToArray(), 0, packet.Length()); }
                /*System.IO.IOException: "Unable to read data from the transport connection: Программа на вашем хост-компьютере разорвала установленное подключение.."*/
                catch (Exception ex) {
                    Server.Logger($"[TCPServer:{id}]Error send: {ex}."); }
            }

            public void Disconnect()
            {
                if(client != null)
                {
                    Server.Logger($"[TCPServer:{client.Client.RemoteEndPoint}]Disconnect.");
                    thisClient.disconnectPoint = client.Client.RemoteEndPoint;
                    client.Close();
                    client = null;
                }
                if(stream != null)
                {
                    stream.Close();
                    stream = null;
                }
                recivedPacket = null;
            }
        }
        public class UDP
        {
            private readonly int id;
            public IPEndPoint endPoint;


            public UDP(int id)
            {
                this.id = id;
            }

            public void Connect(IPEndPoint endPoint)
            {
                this.endPoint = endPoint;
            }

            public void Send(Packet packet)
            {
                Server.SendUDP(endPoint, packet);
            }

            public void HandlePacket(Packet packet)
            {
                int length = packet.ReadInt();
                if (length <= 0) return;
                
                int packetId = packet.ReadInt();
                packetHandlers[packetId](id, packet);
                    
            }

            public void Disconnect()
            {
                Server.Logger($"[UDPServer:{endPoint}]Disconnect.");
                endPoint = null;
            }
        }

        //=================================================Handler messages======================================================\\
        private delegate void PacketHandler(int idClient, Packet packet);
        private static Dictionary<int, PacketHandler> packetHandlers;
        private void InitializeClientData()
        {
            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                {(int)ClientPackets.reconnect, HandlePacket.Reconnect },
                {(int)ClientPackets.registration, HandlePacket.Registration },
                {(int)ClientPackets.authorization, HandlePacket.Authorization },

                {(int)ClientPackets.createPrivateRoom, HandlePacket.CreatePrivateRoom },
                {(int)ClientPackets.findPrivateRoom, HandlePacket.FindPrivateRoom },

                {(int)ClientPackets.spawnPlayer, HandlePacket.SpawnPlayer },
                {(int)ClientPackets.positionAndRotation, HandlePacket.PositionAndRotation },

                {(int)ClientPackets.throwAttackCard, HandlePacket.ThrowAttackCard }
            };
        }

        /*public async void Write(Packet packet)
        {
            try
            {
                if (socket != null)
                {
                    await stream.WriteAsync(packet.ToArray(), 0, packet.Length());//System.IO.IOException: "Unable to read data from the transport connection: Программа на вашем хост-компьютере разорвала установленное подключение.."
                }
            }
            catch (Exception ex)
            {
                Server.Logger($"[Server]Error sending data to client {Id} via TCP: {ex}");
            }
        }
        private async void Read(NetworkStream stream)
        {
            try
            {
                byte[] data = new byte[64];
                int length = await stream.ReadAsync(data, 0, 64);// System.StackOverflowException
                if(length <= 0)
                {
                    Disconnect();
                    return;
                }

                byte[] packet = new byte[length];
                Array.Copy(data, packet, length);

                recivedPacket.Reset(HandleRecivedData(packet));
                Read(stream);
            }
            catch (Exception ex)
            {
                Server.Logger($"[Server:{Id}]Can`t read: {ex}.\nDisconnect {socket.Client.RemoteEndPoint}.");
                Disconnect();
            }
        }*/
    }
}
