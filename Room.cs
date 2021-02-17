using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Room
    {
        public readonly int id;
        public readonly string name;
        public readonly string password;
        public readonly bool isPublic;
        public Dictionary<int, Player> players = new Dictionary<int, Player>();
        public Dictionary<int, Player> queue = new Dictionary<int, Player>();

        public Game Game { get; private set; }

        private bool playNow;

        public Room(int id, string name, string password)
        {
            this.id = id;
            this.name = name;
            this.password = password;
            isPublic = false;
        }        
        public Room(int id, string name)
        {
            this.id = id;
            this.name = name;
            isPublic = true;
        }

        internal async void Enter(Player player, int idPlayer)
        {
            Server.clients[idPlayer].ChangeRoom(id);

            if (Game != null) queue.Add(idPlayer, player); 
            else
            {
                players.Add(idPlayer, player);
                if (players.Count >= 2)
                    Game = await Task.Run(() => new Game(this));
            }

        }
        internal Player Exit(int id)
        {
            Player player = players[id];
            players.Remove(id);
            return player;
        }
    }
}
