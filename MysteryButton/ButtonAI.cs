﻿using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using Logger = BepInEx.Logging.Logger;
using Random = System.Random;

namespace MysteryButton
{
    public class ButtonAI : EnemyAI, INetworkSerializable
    {
        private static int cpt = 0;

        internal static ManualLogSource logger = Logger.CreateLogSource("Elirasza.MysteryButton.ButtonAI");

        private Random rng;

        private bool isLock;

        private int id;

        public override void Start()
        {
            logger.LogInfo("ButtonAI::Start");

            id = cpt++;
            enemyHP = 100;
            rng = new Random((int)NetworkObjectId);
            isLock = false;
            base.Start();
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);

            if (!isLock)
            {
                logger.LogInfo("ButtonAI::OnCollideWithPlayer, ButtonAI::id=" + id);
                isLock = true;
                PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();

                if (player != null)
                {
                    logger.LogInfo("ButtonAI::OnCollideWithPlayer, player EXISTS for collider " +
                                   other.gameObject.name);

                    int effect = rng.Next(0, 100);
                    logger.LogInfo("effect=" + effect);

                    if (effect < 50)
                    {
                        logger.LogInfo("Bonus effect");
                        DoBonusEffect(player);
                    }
                    else
                    {
                        logger.LogInfo("Malus effect");
                        DoMalusEffect(player);
                    }

                    KillEnemyServerRpc();
                }
            }
        }

        private void DoBonusEffect(PlayerControllerB player)
        {
            // Do nothing
        }

        private void DoMalusEffect(PlayerControllerB player)
        {
            int effect = rng.Next(0, 100);
            logger.LogInfo("Malus effect=" + effect);

            if (effect < 45)
            {
                PlayerDrunkServerRpc();
            }
            else if (effect < 50)
            {
                RandomPlayerIncreaseInsanityServerRpc();
            }
            else
            {
                int open = rng.Next(0, 100);
                if (open < 50)
                {
                    OpenAllDoorsServerRpc(player.name);
                }
                else
                {
                    CloseAllDoorsServerRpc(player.name);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void KillEnemyServerRpc()
        {
            KillEnemyClientRpc();
        }

        [ClientRpc]
        public void KillEnemyClientRpc()
        {
            logger.LogInfo("ButtonAI::KillEnemyClientRpc");
            KillEnemy(true);
        }

        [ServerRpc(RequireOwnership = false)]
        void PlayerDrunkServerRpc()
        {
            PlayerDrunkClientRpc();
        }

        [ClientRpc]
        void PlayerDrunkClientRpc()
        {
            logger.LogInfo("ButtonAI::PlayerDrunkClientRpc");
            if (StartOfRound.Instance != null)
            {
                foreach (PlayerControllerB player in GetActivePlayers())
                {
                    logger.LogInfo("Client: Apply effect to " + player.playerUsername);
                    player.drunkness = 5f;
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void RandomPlayerIncreaseInsanityServerRpc()
        {
            RandomPlayerIncreaseInsanityClientRpc();
        }

        [ClientRpc]
        void RandomPlayerIncreaseInsanityClientRpc()
        {
            logger.LogInfo("ButtonAI::RandomPlayerIncreaseInsanityClientRpc");
            if (StartOfRound.Instance != null)
            {
                PlayerControllerB[] currentPlayers = GetActivePlayers().Where(player => player.playerSteamId != 0).ToArray();
                PlayerControllerB player = currentPlayers[rng.Next(currentPlayers.Length)];
                player.insanityLevel = player.maxInsanityLevel;
                logger.LogInfo("Client: Apply max insanity to " + player.playerUsername);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void OpenAllDoorsServerRpc(string name)
        {
            OpenAllDoorsClientRpc(name);
        }

        [ClientRpc]
        void OpenAllDoorsClientRpc(string name)
        {
            var player = GetPlayerByNameOrFirstOne(name);

            logger.LogInfo("ButtonAI::OpenAllDoorsClientRpc");
            List<DoorLock> doors = FindObjectsOfType<DoorLock>().ToList();
            logger.LogInfo("OpenAllDoors: " + doors.Count);
            foreach (DoorLock door in doors)
            {
                if (!door.isDoorOpened)
                {
                    door.OpenOrCloseDoor(player);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void CloseAllDoorsServerRpc(string name)
        {
            CloseAllDoorsClientRpc(name);
        }

        [ClientRpc]
        void CloseAllDoorsClientRpc(string name)
        {
            var player = GetPlayerByNameOrFirstOne(name);

            logger.LogInfo("ButtonAI::CloseAllDoorsClientRpc");
            List<DoorLock> doors = FindObjectsOfType<DoorLock>().ToList();
            logger.LogInfo("CloseAllDoors: " + doors.Count);
            foreach (DoorLock door in doors)
            {
                if (door.isDoorOpened)
                {
                    door.OpenOrCloseDoor(player);
                }
            }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref id);
        }

        private static PlayerControllerB GetPlayerByNameOrFirstOne(string name)
        {
            List<PlayerControllerB> activePlayers = GetActivePlayers();
            return activePlayers.FirstOrDefault(x => x.name == name) ?? StartOfRound.Instance.allPlayerScripts[0];
        }

        private static List<PlayerControllerB> GetActivePlayers()
        {
            List<PlayerControllerB> activePlayers = [];
            activePlayers.AddRange(StartOfRound.Instance.allPlayerScripts.Where(player => player.isActiveAndEnabled).ToList());
            return activePlayers;
        }
    }
}