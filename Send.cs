using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace Server
{
    public class Send
    {
        private const string welcome = "Your id is:";
        public static void Welcome(int idClient)
        {
            using Packet packet = new Packet((int)ServerPackets.welcome);
            packet.Write(welcome);
            packet.Write(idClient);

            Server.Logger($"[Server:{idClient}{{{(int)ServerPackets.welcome}}}]Send: {welcome} {idClient}.");

            SendData(idClient, packet);
        }

        public static void Authorization(int idClient, string message)
        {
            Server.Logger($"[Server:{idClient}{{{(int)ServerPackets.authorization}}}]Send: {message}");
            Write((int)ServerPackets.authorization, idClient, message);
        }
        public static void Registration(int idClient, string message)
        {
            Server.Logger($"[Server:{idClient}{{{(int)ServerPackets.registration}}}]Send: {message}");
            Write((int)ServerPackets.registration, idClient, message);
        }

        internal static void ConnectToRoom(int idClient, string message)
        {
            Server.Logger($"[Server:{idClient}{{{(int)ServerPackets.connectToRoom}}}]Send: {message}");
            Write((int)ServerPackets.connectToRoom, idClient, message);
        }

        internal static void ErrorFindPrivateRoom(int idClient, string message)
        {
            Server.Logger($"[Server:{idClient}{{{(int)ServerPackets.errorFindPrivateRoom}}}]Send: {message}");
            Write((int)ServerPackets.errorFindPrivateRoom, idClient, message);
        }

        internal static void RoomInfo(int idClient, int idRoom, string nameRoom, int countPlayers)
        {
            using Packet packet = new Packet((int)ServerPackets.roomInfo);
            packet.Write(idRoom);
            packet.Write(nameRoom);
            packet.Write(countPlayers);

            Server.Logger($"[Server:{idClient}{{{(int)ServerPackets.roomInfo}}}]Room info id:{idRoom} name:{nameRoom} count players:{countPlayers}");

            SendData(idClient, packet);
        }

        internal static void SpawnPlayer(int idRoom, int id, string nickName, Vector3 position, Quaternion rotation)
        {
            using (Packet packet = new Packet((int)ServerPackets.spawnPlayer))
            {
                packet.Write(id);
                packet.Write(nickName);
                packet.Write(position);
                packet.Write(rotation);

                Server.Logger($"[Server.{idRoom}{{{(int)ServerPackets.spawnPlayer}}}]Spawn id:{id} nick:{nickName} " +
                    $"position({position.X},{position.Y},{position.Z}) rotation({rotation.X},{rotation.Y},{rotation.Z})");

                SendDataToRoom(packet, idRoom);
            }
            foreach (int idPlayer in Server.rooms[idRoom].players.Keys)
                if (idPlayer != id)
                {
                    using Packet packet = new Packet((int)ServerPackets.spawnPlayer);

                    packet.Write(idPlayer);
                    packet.Write(Server.clients[idPlayer].Name);
                    Vector3 pos = Server.rooms[idRoom].players[idPlayer].lastPosition;
                    packet.Write(pos);
                    Quaternion rot = Server.rooms[idRoom].players[idPlayer].lastRotation;
                    packet.Write(rot);

                    Server.Logger($"[Server:{id}{{{(int)ServerPackets.spawnPlayer}}}]Spawn id:{idPlayer} nick:{Server.clients[idPlayer].Name} " +
                        $"position({pos.X},{pos.Y},{pos.Z})");

                    SendData(id, packet);
                }
        }

        //======================================================Game===================================================\\
        internal static void DistributionCards(int idClient, int idPlayer, List<sbyte> playersDeck)
        {
            using Packet packet = new Packet((int)ServerPackets.distributionPrivateCards);
            packet.Write(idPlayer);
            packet.Write(playersDeck);

            SendData(idClient, packet);
            Server.Logger($"[Server:{idClient}{{{(int)ServerPackets.distributionPrivateCards}}}]Deck player:{idPlayer} length: {playersDeck.Count}");
        }

        internal static void DistributionCards(int idClient, int idPlayer, int count)
        {
            using Packet packet = new Packet((int)ServerPackets.distributionCards);
            packet.Write(idPlayer);
            packet.Write(count);

            SendData(idClient, packet);
            Server.Logger($"[Server:{idClient}{{{(int)ServerPackets.distributionCards}}}]Deck player:{idPlayer} count: {count}");
        }

        internal static void IncomingAndFightOff(int idRoom, int incomingPlayer, int fightOffPlayer)
        {
            using Packet packet = new Packet((int)ServerPackets.incomingAndFightOff);
            packet.Write(incomingPlayer);
            packet.Write(fightOffPlayer);

            SendDataToRoom(packet, idRoom);
            Server.Logger($"[Server.{idRoom}{{{(int)ServerPackets.incomingAndFightOff}}}]Attack: {incomingPlayer}. Defense: {fightOffPlayer}");
        }

        internal static void Trump(int idRoom, sbyte trump)
        {
            using Packet packet = new Packet((int)ServerPackets.trump);
            packet.Write((byte)trump);

            SendDataToRoom(packet, idRoom);
            Server.Logger($"[Server.{idRoom}{{{(int)ServerPackets.trump}}}]Trump is: {trump}");
        }

        internal static void ThrowCard(int idPlayer, sbyte card, sbyte point, bool attack)
        {
            using Packet packet = new Packet((int)ServerPackets.throwCard);
            packet.Write(idPlayer);
            packet.Write((byte)card);
            packet.Write((byte)point);
            packet.Write(attack);

            SendDataToRoom(packet, Server.clients[idPlayer].IdRoom);
        }

        //==================================UDP=======================================\\
        internal static void PositionAndRotation(int idClient, Packet packet)
        {
            packet.RemoveInt();
            SendUDPToRoom(packet, Server.clients[idClient].IdRoom, idClient);
        }



        //=================================Functions========================================\\
        private static void Write(int idPacket,int idClient, string message)
        {
            using Packet packet = new Packet(idPacket);
            packet.Write(message);

            SendData(idClient, packet);
        }

        //===============================================Send====================================\\
        private static void SendData(int idClient, Packet packet)
        {
            packet.WriteLength();
            Server.clients[idClient].tcp.Write(packet);
        }
        private static void SendDataToRoom(Packet packet, int idRoom)
        {
            packet.WriteLength();
            foreach (int id in Server.rooms[idRoom].players.Keys)
                Server.clients[id].tcp.Write(packet);
        }
        private static void SendDataToRoom(Packet packet, int idRoom, int idExceptionClient)
        {
            packet.WriteLength();
            foreach (int id in Server.rooms[idRoom].players.Keys)
                if(id != idExceptionClient)
                Server.clients[id].tcp.Write(packet);
        }
        private static void SendUDPToRoom(Packet packet, int idRoom, int idExceptionClient)
        {
            packet.WriteLength();
            foreach (int id in Server.rooms[idRoom].players.Keys)
                if(id != idExceptionClient)
                Server.clients[id].udp.Send(packet);
        }
    }
}
