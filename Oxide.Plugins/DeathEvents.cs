using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Facepunch.Extend;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("DeathEvents", "Katakurinna", "1.0.0")]
    [Description("Deaths event system")]
    class DeathEvents : RustPlugin
    {
        DeathEventsConfig Settings { get; set; }

        private void Init()
        {
            permission.RegisterPermission("deathevents.stats", this);
            permission.RegisterPermission("deathevents.podium", this);
            AddCovalenceCommand("stats", "StatsCommand");
            AddCovalenceCommand("podium", "PodiumCommand");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Settings = Config.ReadObject<DeathEventsConfig>();
            SaveConfig();
        }

        /// <summary>
        /// Oxide hook that is triggered to automatically load the default configuration file when no file exists.
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            Settings = DeathEventsConfig.DefaultConfig();
        }

        void BroadcastChat(string msg)
        {
            if (msg == null) return;
            PrintToChat(msg);
        }

        object OnPlayerDeath(BasePlayer victim, HitInfo info)
        {
            if (victim.lastAttacker is BaseTrap) return KilledByLandmine(victim, info);

            var killer = info?.Initiator as BasePlayer;
            if (killer == null || killer == victim) return null;
            if (victim.IsNpc || killer.IsNpc) return null;

            return KilledByPlayer(victim, killer, info);
        }

        object KilledByPlayer(BasePlayer victim, BasePlayer killer, HitInfo info)
        {
            var victimName = victim.displayName;
            var killerName = killer.displayName;
            var mocha = info.isHeadshot ? "Yay" : "Nay";
            BroadcastChat("<color=#a52a2aff>" + killerName + "</color> kills <color=red>" + victimName +
                          "</color> (" + info.ProjectileDistance.ToString("0.00")
                          + "m) Hs: " + mocha);
            SendPlayerDeathEventsEvent(victim, killer, info);
            return null;
        }

        object KilledByLandmine(BasePlayer victim, HitInfo info)
        {
            if (!(victim.lastAttacker is BaseTrap)) return null;

            var trap = (BaseTrap)victim.lastAttacker;
            if (!trap.ShortPrefabName.Contains("landmine")) return null;

            var victimName = victim.displayName;
            BroadcastChat("<color=red>" + victimName + "</color> sa comio una wena polla.");
            return null;
        }

        void SendPlayerDeathEventsEvent(BasePlayer victim, BasePlayer killer, HitInfo info)
        {
            var killEvent = new PlayerDeathEventsEvent();
            killEvent.player = new Player(victim.displayName, victim.UserIDString);
            killEvent.killer = new Player(killer.displayName, killer.UserIDString);
            killEvent.distance = info.ProjectileDistance.ToString("0.00").ToFloat();
            killEvent.headshot = info.isHeadshot;
            killEvent.weapon = info.Weapon.ShortPrefabName;

            var eventString = JsonConvert.SerializeObject(killEvent);
            Puts(eventString);
        }

        object OnUserChat(IPlayer player, string message)
        {
            if (message.Equals("!podium"))
            {
                webrequest.Enqueue(Settings.RequestUrl + "podium?serverId=" + Settings.ServerId, null,
                    (code, response) => GeneratePodium(code, response), this);
            }
            else if (message.Equals("!stats"))
            {
                var playerId = player.Id;
                webrequest.Enqueue(
                    Settings.RequestUrl + "stats?steamId=" + playerId + "&serverId=" + Settings.ServerId,
                    null,
                    (code, response) => GenerateStats(code, response, player), this);
            }

            return null;
        }

        private void GenerateStats(int code, string response, IPlayer player)
        {
            if (code != 200 || response == null)
            {
                Puts($"Couldn't get an answer from rust statistics!");
                return;
            }

            Stats stats = JsonConvert.DeserializeObject<Stats>(response);
            PrintStats(stats, player);
        }

        private void PrintStats(Stats stats, IPlayer player)
        {
            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Local);
            var time = posixTime.AddMilliseconds(stats.player.creationDate).ToString("dd/MM/yyyy HH:mm:ss");
            string msg = "Stats for " + stats.player.name + ":";
            msg += "\n Total Kills: " + stats.totalKills;
            msg += "\n Wipe Kills: " + stats.wipeKills;
            msg += "\n Total Deaths: " + stats.totalDeaths;
            msg += "\n Wipe Deaths: " + stats.wipeDeaths;
            msg += "\n Total KDR: " + stats.totalKDR;
            msg += "\n Wipe KDR: " + stats.wipeKDR;
            msg += "\n Average headshot: " + stats.averageHeadshot;
            msg += "\n Most used weapon: " + stats.mostUsedWeapon;
            msg += "\n Longest kill: " + stats.furthestMurder;
            msg += "\n First seen in server: " + time;
            player.Reply(msg);
        }

        private void GeneratePodium(int code, string response)
        {
            if (code != 200 || response == null)
            {
                Puts($"Couldn't get an answer from rust statistics!");
                return;
            }

            Podium podium = JsonConvert.DeserializeObject<Podium>(response);
            PrintPodium(podium);
        }

        #region Commands

        [Command("podium"), Permission("deathevents.podium")]
        private void PodiumCommand(IPlayer player, string command, string[] args)
        {
            var podiumUrl = Settings.RequestUrl + "podium?serverId=" + Settings.ServerId;
            webrequest.Enqueue(podiumUrl, null, (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    Puts($"Couldn't get an answer from rust statistics!");
                    return;
                }

                Podium podium = JsonConvert.DeserializeObject<Podium>(response);
                PrintPodium(podium);
            }, this);
        }

        [Command("stats"), Permission("deathevents.stats")]
        private void StatsCommand(IPlayer iplayer, string command, string[] args)
        {
            if (iplayer == null) return;
            var playerId = iplayer.Id;
            webrequest.Enqueue(
                Settings.RequestUrl + "stats?steamId=" + playerId + "&serverId=" + Settings.ServerId,
                null,
                (code, response) => GenerateStats(code, response, iplayer), this);
        }

        #endregion

        void PrintPodium(Podium podium)
        {
            Puts("Podium:" + podium.podium.Count);
            string msg = "<color=red>TOP KILLERS</color>\n";
            foreach (var podiumPlayer in podium.podium)
            {
                msg += podiumPlayer.podiumPosition + ". <color=red>" + podiumPlayer.name + "</color> (" +
                       podiumPlayer.kills + " kills)\n";
            }

            BroadcastChat(msg);
        }

        // void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        // {
        //     //Puts(entity.name + " has been killed by " + info.InitiatorPlayer  + ". Mocha: " + info.isHeadshot);
        // }
        sealed class PodiumPlayer
        {
            [JsonProperty("name")] public string name { get; set; }

            [JsonProperty("steamID")] public object steamID { get; set; }

            [JsonProperty("podiumPosition")] public int podiumPosition { get; set; }

            [JsonProperty("kills")] public int kills { get; set; }

            [JsonProperty("deaths")] public int deaths { get; set; }
        }

        sealed class Podium
        {
            [JsonProperty("podium")] public List<PodiumPlayer> podium { get; set; }
        }

        sealed class PlayerStats
        {
            public int id { get; set; }
            public long steamId { get; set; }
            public string name { get; set; }
            public object discord { get; set; }
            public long creationDate { get; set; }
            public long lastJoinDate { get; set; }
        }

        sealed class Stats
        {
            public PlayerStats player { get; set; }
            public string avatar { get; set; }
            public int wipeKills { get; set; }
            public int wipeDeaths { get; set; }
            public string wipeKDR { get; set; }
            public int totalKills { get; set; }
            public int totalDeaths { get; set; }
            public string totalKDR { get; set; }
            public object furthestMurder { get; set; }
            public object averageHeadshot { get; set; }
            public object mostUsedWeapon { get; set; }
        }


        sealed class Player
        {
            public Player(string username, string steamID)
            {
                this.username = username;
                this.steamID = steamID;
            }

            public string username { get; set; }

            public string steamID { get; set; }
        }

        sealed class PlayerDeathEventsEvent
        {
            public Player player { get; set; }
            public Player killer { get; set; }
            public float distance { get; set; }
            public bool headshot { get; set; }
            public string weapon { get; set; }
        }


        /// <summary>
        /// Plugin configuration
        /// </summary>
        public class DeathEventsConfig
        {
            [JsonProperty("Server Manager Server Id")]
            public int ServerId { get; set; }

            [JsonProperty("Server Manager Server Wipe Id")]
            public int ServerWipeId { get; set; }

            [JsonProperty("Server Manager API URL")]
            public string RequestUrl { get; set; }

            /// <summary>
            /// Creates a default configuration file
            /// </summary>
            /// <returns>Default config</returns>
            public static DeathEventsConfig DefaultConfig()
            {
                return new DeathEventsConfig
                {
                    ServerId = 1,
                    ServerWipeId = 1,
                    RequestUrl = "http://rust.cerratolabs.me:8080/"
                };
            }
        }
    }
}