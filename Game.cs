using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Server
{
    class Game
    {
        private Room room;

        //Card
        private readonly List<sbyte> deck = new List<sbyte>();
        private readonly sbyte trump = 0;
        //Edited
        private bool firstDefense = true;
        private sbyte index = 0;
        private readonly Dictionary<int, List<sbyte>> playersDeck = new Dictionary<int, List<sbyte>>();
        private Dictionary<sbyte, sbyte> tableCard = new Dictionary<sbyte, sbyte>();
        private bool fullAtack;


        //Player
        private readonly List<int> queue = new List<int>();
        //Edited
        private int attackPlayer = -1, defensePlayer = -1;
        private Dictionary<int, bool> playersPass = new Dictionary<int, bool>();
        private bool throwin, fullPass;



        internal Game(Room room)
        {
            Thread.Sleep(10000); //await Task.Delay(100);
            this.room = room;
            foreach (int id in room.players.Keys)
            {
                queue.Add(id);
                playersPass.Add(id, false);
                playersDeck.Add(id, new List<sbyte>());
            }

            //Перетасовка карт
            deck = ShuffleDeck(1);
            //Выбор козыря
            trump = deck[35];
            Send.Trump(room.id, trump);
            //Раздача
            Distribution();

            attackPlayer = IndexFirstAttackPlayer(playersDeck, trump);

            int queuePlace = queue.IndexOf(attackPlayer) + 1;
            if (queuePlace == queue.Count) defensePlayer = queue[0];
            else defensePlayer = queue[queuePlace];


            //Tag: Debug
            Console.WriteLine($"Attack player: {attackPlayer}. Defense player: {defensePlayer}.");

            Send.IncomingAndFightOff(room.id, attackPlayer, defensePlayer);
        }

        internal void ThrowAttackCard(int idPlayer, sbyte card)
        {
            Server.Logger($"[Room:{room.id}]Player[{idPlayer}] want throw attack card: {card}.");
            if (idPlayer != attackPlayer)
                if (!throwin)
                    return;

            if (tableCard.Count != 0)
                if (fullAtack)
                    return;
                else if (!OnTableSameRankCard(card))
                    return;

            if (playersDeck[idPlayer].IndexOf(card) == -1)
                return;

            playersPass[idPlayer] = false;

            //Tag: Debug
            Server.Logger($"[Room:{room.id}]Player[{idPlayer}]Throw attack card: {card}.");

            tableCard.Add(card, -1);
            playersDeck[idPlayer].Remove(card);
            Send.ThrowCard(idPlayer, card, (sbyte)(tableCard.Count - 1), true);

            FullAttack();
        }

        internal void ThrowDefenseCard(int idPlayer, sbyte defenseCard, sbyte attackCard)
        {
            Server.Logger($"[Room:{room.id}]Player[{idPlayer}] want throw defense card: {defenseCard} on attack card: {attackCard}.");
            if (idPlayer != defensePlayer)
                return;

            if (!tableCard.ContainsKey(attackCard))
                return;

            if (playersDeck[idPlayer].IndexOf(defenseCard) == -1)
                return;

            if (!DefendingCard(defenseCard, attackCard, trump))
                return;

            //Tag: Debug
            Server.Logger($"[Room:{room.id}]Player[{idPlayer}]Throw defense card: {defenseCard} on attack card: {attackCard}.");

            tableCard[attackCard] = defenseCard;
            playersDeck[idPlayer].Remove(defenseCard);

            //Send throw card
        }

        internal void Pass(int idPlayer)
        {
            Server.Logger($"[Room:{room.id}]Player[{idPlayer}]Want pass.");
            if (idPlayer == defensePlayer)
                if (tableCard.Count == 0)
                    return;
                else
                {
                    PickUp(idPlayer);
                    return;
                }

            if (!throwin)
                if (idPlayer == attackPlayer)
                    if (tableCard.Count == 0)
                        return;
                    else
                        throwin = true;
                else return;

            //Tag: Debug
            Server.Logger($"[Room:{room.id}]Player[{idPlayer}]Pass.");
            playersPass[idPlayer] = true;
            CheckPassPlayer();
        }
        protected void CheckPassPlayer()
        {
            sbyte countPassPlayer = 0;
            foreach (KeyValuePair<int, bool> player in playersPass)
                if (player.Key != defensePlayer)
                    if (player.Value == true)
                        countPassPlayer++;

            if (countPassPlayer == playersPass.Count - 1)
            {
                fullPass = true;
                EndStep();
            }
        }

        protected void EndStep()
        {
            if (!tableCard.ContainsValue(-1) && (fullAtack || fullPass))
                HangUp();
        }

        protected void HangUp()//Отбой
        {
            //Tag: Debug
            Console.WriteLine("Hang up.");
            if (firstDefense)
                firstDefense = false;
            NextStep(true);
        }

        protected void PickUp(int idPlayer)//Забрал
        {
            //Tag: Debug
            Console.WriteLine("Player: " + idPlayer + " pickup.");
            List<sbyte> pickUpCard = new List<sbyte>();
            foreach (KeyValuePair<sbyte, sbyte> player in tableCard)
            {
                pickUpCard.Add(player.Key);
                if (player.Value != -1)
                    pickUpCard.Add(player.Value);
            }
            playersDeck[idPlayer].AddRange(pickUpCard);

            //TODO: Отправить забраные карты

            NextStep(false);
        }
        protected void NextStep(bool hangUp)
        {
            Distribution();

            int queuePlace = queue.IndexOf(defensePlayer);
            if (!hangUp)
                if(++queuePlace == queue.Count)
                    queuePlace = 0;
            
            //Find attack player
            while (playersDeck[queue[queuePlace]].Count < 1)
                if (++queuePlace == queue.Count)
                    queuePlace = 0;
            attackPlayer = queue[queuePlace];

            //Find defense player
            while (playersDeck[queue[queuePlace]].Count < 1)
                if (++queuePlace == queue.Count)
                    queuePlace = 0;
            defensePlayer = queue[queuePlace];

            //Reset
            playersPass = new Dictionary<int, bool>();
            foreach (int id in queue)
                playersPass.Add(id, false);
            tableCard = new Dictionary<sbyte, sbyte>();
            throwin = false; fullPass = false; fullAtack = false;

            //Send.IncomingAndFightOff(room.id, attackPlayer, defensePlayer);
        }

        protected void Distribution()
        {
            foreach (KeyValuePair<int, List<sbyte>> player in playersDeck)
            {
                List<sbyte> addedCard = new List<sbyte>();
                while (player.Value.Count < 6 && index < deck.Count)
                {
                    player.Value.Add(deck[index]);
                    addedCard.Add(deck[index]);
                    
                    //Tag: Debug
                    Console.Write(deck[index] + ", ");
                    index++;
                }

                //Tag: Debug
                Console.WriteLine();

                foreach(KeyValuePair<int, List<sbyte>> recivePlayer in playersDeck)
                    if(recivePlayer.Key == player.Key)
                        Send.DistributionCards(recivePlayer.Key, player.Key, addedCard);
                    else
                        Send.DistributionCards(recivePlayer.Key, player.Key, addedCard.Count);
            }

           Server.Logger("   Game.Distribution cards successful.");
        }
        protected List<sbyte> ShuffleDeck(sbyte countDeck)
        {
            int countCards = countDeck * 36;
            List<sbyte> deck = new List<sbyte>();

            //Tag: Debug
            Console.Write("Deck: ");

            Random random = new Random();
            for (int i = 0; i < countCards; i++)
            {
                bool correct = true;
                while (correct)
                {
                    sbyte card = (sbyte)random.Next(36);
                    if (deck.IndexOf(card) == -1)
                    {
                        deck.Add(card);

                        //Tag: Debug
                        Console.Write(card + ", ");

                        correct = false;
                    }
                }
            }

            //Tag: Debug
            Console.WriteLine();

            return deck;
        }
        protected int IndexFirstAttackPlayer(Dictionary<int, List<sbyte>> playersDeck, sbyte trump)
        {
            //Создаем массив для каждого игрока минимальных карт
            Dictionary<int, sbyte> minCard = new Dictionary<int, sbyte>();
            int[] mincard = new int[playersDeck.Count];

            //Забиваем числами больше тех что в колоде
            for (int i = 0; i < mincard.Length; i++)
                mincard[i] = 1000;

            //Ищем минимальные карты у каждого игрока
            foreach (KeyValuePair<int, List<sbyte>> player in playersDeck)
                foreach (sbyte card in player.Value)
                    if (EqualSuit(card, trump))
                        if (!minCard.ContainsKey(player.Key))
                            minCard.Add(player.Key, card);
                        else if (card < minCard[player.Key]) minCard[player.Key] = card;

            //Проверяем у какого игрока карта меньше
            return minCard.Where(e => e.Value == minCard.Min(x => x.Value)).First().Key; //System.InvalidOperationException: "Sequence contains no elements"
        }



        protected bool FullAttack()
        {
            int countAttackCard = tableCard.Count;
            if (countAttackCard >= playersDeck[defensePlayer].Count ||
                (firstDefense && countAttackCard >= 5) || countAttackCard >= 6)
            {
                //Server.Logger("GameServer.Full card attack, attack card: " + countAttackCard + ", fightoff deck: " +
                //    playersDeck[defensePlayer].Count + ", first defense: " + firstDefense + ".");
                fullAtack = true;
                return true;
            }

            return false;
        }
        protected bool DefendingCard(sbyte defendCard, sbyte attackCard, sbyte trump)
        {
            //Если козырь бьет ли карту
            if (EqualSuit(defendCard, trump) &&
                (defendCard > attackCard || !EqualSuit(trump, attackCard)))
                return true;
            //Если не козырь бьет ли карту
            else
                if (EqualSuit(defendCard, attackCard) && defendCard > attackCard)
                return true;
            return false;
        }
        protected bool OnTableSameRankCard(sbyte throwCard)
        {
            foreach (KeyValuePair<sbyte, sbyte> pair in tableCard)
            {
                if (EqualRank(pair.Key, throwCard))
                    return true;
                if (pair.Value != -1 && EqualRank(pair.Value, throwCard))
                    return true;
            }

            return false;
        }


        //Проверяет равенство рангов двух карт 7Ч и 7К равны 7Ч и 8Ч неравны
        protected bool EqualRank(sbyte firstCard, sbyte secondCard)
        {
            double firstRank = firstCard / 4;
            double secondRank = secondCard / 4;

            if (Math.Truncate(firstRank) == Math.Truncate(secondRank)) return true;
            else return false;
        }


        //Проверяет равенство мастей двух карт 7Ч и 7К неравны 7Ч и 8Ч равны
        protected bool EqualSuit(int firstCard, int secondCard)
        {
            if (firstCard > secondCard)
            {
                while (firstCard > secondCard)
                {
                    firstCard -= 4;
                    if (firstCard == secondCard) return true;
                }
                return false;
            }
            else
            {
                if (firstCard == secondCard) return true;
                else
                {
                    while (secondCard > firstCard)
                    {
                        secondCard -= 4;
                        if (firstCard == secondCard) return true;
                    }
                    return false;
                }
            }
        }
    }
}
