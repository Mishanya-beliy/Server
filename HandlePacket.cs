using System;
using System.Numerics;

namespace Server
{
    class HandlePacket
    {

        internal static void Reconnect(int idClient, Packet packet)
        {
            int disconnectId = packet.ReadInt();
            string disconnectPoint = packet.ReadString();

            if (Server.clients.ContainsKey(disconnectId))
                Server.clients[disconnectId].Reconnect(disconnectPoint, idClient);
        }
        #region RegAndAuth
        public static void Registration(int idClient, Packet packet)
        {
            string login = packet.ReadString();
            string password = packet.ReadString();

            Server.Logger($"[Client {idClient}]Want registration with login: {login} password: {password}");

            Authorizating.Registration(idClient, login, password);
        }
        public static void Authorization(int idClient, Packet packet)
        {
            string login = packet.ReadString();
            string password = packet.ReadString();

            Server.Logger($"[Client {idClient}]Want authorizating with login: {login} password: {password}");

            Authorizating.Authorization(idClient, login, password);
        }
        #endregion

        #region Room
        internal static void FindPrivateRoom(int idClient, Packet packet)
        {
            string[] message = Read(packet);
            Server.Logger($"[Client {idClient}]Want find private room name: {message[0]} password: {message[1]}");

            Server.FindPrivateRoom(idClient, message[0], message[1]);
        }

        internal static void CreatePrivateRoom(int idClient, Packet packet)
        {
            string[] message = Read(packet);
            Server.Logger($"[Client {idClient}]Want create private room name: {message[0]} password: {message[1]}");

            Server.CreateRoom(idClient, message[0], message[1]);
        }
        #endregion

        #region Player
        internal static void SpawnPlayer(int idClient, Packet packet)
        {
            int id = packet.ReadInt();
            string nickName = packet.ReadString();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();

            Server.Logger($"[Client {idClient}]Want spawn player: {id} nick name: {nickName} in position ({position.X}, {position.Y}, {position.Z})");

            Send.SpawnPlayer(Server.clients[idClient].IdRoom, id, nickName, position, rotation);
        }

        internal static void PositionAndRotation(int idClient, Packet packet)
        {
            Send.PositionAndRotation(idClient, packet);
            Server.clients[idClient].AddSyncPackage();
        }
        #endregion

        #region Game

        internal static void ThrowAttackCard(int idClient, Packet packet)
        {
            sbyte card = (sbyte)packet.ReadByte();

            Server.rooms[Server.clients[idClient].IdRoom].Game.ThrowAttackCard(idClient, card);
        }
        #endregion
        private static string[] Read(Packet packet)
        {
            return new string[]
            { 
                packet.ReadString(),
                packet.ReadString()
            };
        }
    }
}
