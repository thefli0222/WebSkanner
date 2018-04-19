using DemoInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ECO
{
    class ParserThread
    {
        private Dictionary<long, PlayerData> playerData;
        private static List<long> playerThreads;
        private Dictionary<string, List<long>> playersInFile;
        private List<string> activeGames;
        //add active games to just check the other games.
        public ParserThread(String filePath, String fileType)
        {
            string[] filePaths = Directory.GetFiles(filePath, fileType);
            playersInFile = new Dictionary<string, List<long>>();
            foreach (string s in filePaths)
            {
                addPlayers(s);
            }

            playerData = new Dictionary<long, PlayerData>();
            playerThreads = new List<long>();
            activeGames = new List<string>();
            Thread[] tt = new Thread[1];
            

            Boolean haveBeenAdded;
            string fileName;
            

            foreach(string key in playersInFile.Keys)
            {
                for (int i = 0; i < tt.Length; i++)
                {
                    if (tt[i] == null || !tt[i].IsAlive)
                    {
                        if (!isThePlayerActive(key))
                        {
                            Console.WriteLine(key);
                            tt[i] = new Thread(delegate ()
                            {
                                getInfoFromFile(key);
                            });
                            tt[i].Start();
                            activeGames.Add(key);
                            haveBeenAdded = true;
                        }
                    }
                }
            }

            /*
            while (playersInFile.Count > 0)
            {
                for (int x = 0; x < listOfFiles.Count; x++)
                {
                    fileName = (string) listOfFiles[x];
                    haveBeenAdded = false;
                    for (int i = 0; i < tt.Length; i++) {
                        if(tt[i] == null || !tt[i].IsAlive){
                            if (!isThePlayerActive(fileName)) {
                                Console.WriteLine(fileName);
                                tt[i] = new Thread(delegate ()
                                {
                                    getInfoFromFile(fileName);
                                });
                                tt[i].Start();
                                haveBeenAdded = true;
                                
                            }
                        }      
                    }
                    if (!haveBeenAdded)
                    {
                        if (!tempList.Contains(fileName))
                            tempList.Add(fileName);
                    }
                }
                listOfFiles = tempList;
                tempList = new ArrayList();
            } /*


            /*
            foreach (var fileName in filePaths)
            {
                getInfoFromFile(fileName);
            } 

            */
            Boolean test;
            test = true;
            while (test)
            {
                test = false;
                foreach(Thread t in tt)
                {
                    if (t != null && t.IsAlive) test = true;
                }
            }

            foreach (var k in playerData.Keys){
                Console.WriteLine(playerData[k]);
                Console.WriteLine(playerData[k].statString());
            }
        }

        private void addPlayers(String fileName)
        {
            var parser = new DemoParser(File.OpenRead(fileName));
            parser.ParseHeader();
            int count = 0;
            while (count < 10)
            {
                for(int i = 0; i < 100; i++) {
                    parser.ParseNextTick();
                }
                count = 0;
                foreach (var p in parser.PlayingParticipants)
                {
                    count++;
                }
            }
            List<long> tempList = new List<long>();
            foreach (var player in parser.PlayingParticipants)
            {
                tempList.Add(player.SteamID);
            }
            playersInFile.Add(fileName, tempList);
        }

        private bool isThePlayerActive(String fileName)
        {

            foreach(string key in playersInFile.Keys)
            {

            }
            foreach (long testPlayer in playersInFile[fileName])
            {
                foreach(string key in activeGames)
                {
                        foreach(long otherPlayer in playersInFile[key])
                        {
                            if(otherPlayer == testPlayer)
                            {
                                return true;
                            }
                        }
                }
            }
            return false;
        }

        public void getInfoFromFile(string fileName)
        {
            Dictionary<string, int> players = new Dictionary<string, int>();
            Boolean hasMatchStarted;
            hasMatchStarted = false;
            var parser = new DemoParser(File.OpenRead(fileName));
            parser.ParseHeader();
            parser.MatchStarted += (sender, e) => {
                hasMatchStarted = true;
            };

            parser.RoundEnd += (sender, e) => {
                if (!hasMatchStarted)
                    return;

                foreach(Player p in parser.PlayingParticipants)
                {
                    if (p.SteamID != 0)
                    {
                        if (!playerData.ContainsKey(p.SteamID))
                        {
                            playerData.Add(p.SteamID, new PlayerData(p.SteamID));
                        }
                        playerData[p.SteamID].addRound(parser.Map, p.Team, 1);
                        if (!p.IsAlive)
                        {
                            playerData[p.SteamID].addNumber(parser.Map, PlayerData.STAT.DEATH, p.Team, 1);
                        }
                    }
                }
                // We do this in a method-call since we'd else need to duplicate code
                // The much parameters are there because I simply extracted a method
                // Sorry for this - you should be able to read it anywys :)
                //Console.WriteLine("New round");
            };

            parser.PlayerKilled += (sender, e) =>
            {
                if (!hasMatchStarted || e.Killer == null || e.Killer.SteamID == 0)
                        return;
                Player killer = e.Killer;
                if (!playerData.ContainsKey(killer.SteamID))
                {
                    playerData.Add(e.Killer.SteamID, new PlayerData(killer.SteamID));
                }
                playerData[killer.SteamID].addNumber(parser.Map, PlayerData.STAT.KILL, killer.Team, 1);

                //Killing methods
                if(killer.ActiveWeapon != null) {
                    entryFrag(killer, parser);
                    sniperKill(killer, parser);
                    rifleKill(killer, parser);
                    SMGKill(killer, parser);
                    pistolKill(killer, parser);
                }

            };

            parser.SmokeNadeEnded += (sender, e) =>
            {
                if (!hasMatchStarted || e.ThrownBy == null || e.ThrownBy.SteamID == 0)
                    return;
                Player thrower = e.ThrownBy;
                if (!playerData.ContainsKey(thrower.SteamID))
                {
                    playerData.Add(e.ThrownBy.SteamID, new PlayerData(thrower.SteamID));
                }

                playerData[thrower.SteamID].addNumber(parser.Map, PlayerData.STAT.SMOKE, thrower.Team, 1);
            };

            parser.FireNadeEnded += (sender, e) =>
            {
                if (!hasMatchStarted || e.ThrownBy == null || e.ThrownBy.SteamID == 0)
                    return;
                Player thrower = e.ThrownBy;
                if (!playerData.ContainsKey(thrower.SteamID))
                {
                    playerData.Add(e.ThrownBy.SteamID, new PlayerData(thrower.SteamID));
                }

                playerData[thrower.SteamID].addNumber(parser.Map, PlayerData.STAT.MOLOTOV, thrower.Team, 1);
            };

            parser.FlashNadeExploded += (sender, e) =>
            {
                if (!hasMatchStarted || e.ThrownBy == null || e.ThrownBy.SteamID == 0)
                    return;
                Player thrower = e.ThrownBy;
                if (!playerData.ContainsKey(thrower.SteamID))
                {
                    playerData.Add(e.ThrownBy.SteamID, new PlayerData(thrower.SteamID));
                }
               
                playerData[thrower.SteamID].addNumber(parser.Map, PlayerData.STAT.FLASH, thrower.Team, 1);
            };

            parser.ExplosiveNadeExploded += (sender, e) =>
            {
                if (!hasMatchStarted || e.ThrownBy == null || e.ThrownBy.SteamID == 0)
                    return;
                Player thrower = e.ThrownBy;
                if (!playerData.ContainsKey(thrower.SteamID))
                {
                    playerData.Add(e.ThrownBy.SteamID, new PlayerData(thrower.SteamID));
                }

                playerData[thrower.SteamID].addNumber(parser.Map, PlayerData.STAT.GRANADE, thrower.Team, 1);
            };

            //lock (this)
            {
                parser.ParseToEnd();
            }
            
        }

        public Dictionary<long, PlayerData> getPlayerData()
        {
            return playerData;
        }

        //Kill methods
        public void entryFrag(Player killer, DemoParser parser){
            //Is it the frag an entry frag?
            int i = 0;
            foreach (Player p in parser.PlayingParticipants)
            {
                if (p.IsAlive) i++;
            }
            if (i == 1 && killer.Team == DemoInfo.Team.Terrorist)
                playerData[killer.SteamID].addNumber(parser.Map, PlayerData.STAT.ENTRY_FRAG, killer.Team, 1);
        }
        public void SMGKill(Player killer, DemoParser parser){
            if (getWeaponType(killer.ActiveWeapon.Weapon) == 1) 
                playerData[killer.SteamID].addNumber(parser.Map, PlayerData.STAT.SMG_FRAG, killer.Team, 1);
        }
        public void rifleKill(Player killer, DemoParser parser)
        {
            if (getWeaponType(killer.ActiveWeapon.Weapon) == 0)
                playerData[killer.SteamID].addNumber(parser.Map, PlayerData.STAT.RIFLE_FRAG, killer.Team, 1);
        }
        public void sniperKill(Player killer, DemoParser parser)
        {
            if (getWeaponType(killer.ActiveWeapon.Weapon) == 2)
                playerData[killer.SteamID].addNumber(parser.Map, PlayerData.STAT.SNIPER_FRAG, killer.Team, 1);
        }
        public void pistolKill(Player killer, DemoParser parser)
        {
            if (getWeaponType(killer.ActiveWeapon.Weapon) == 3)
                playerData[killer.SteamID].addNumber(parser.Map, PlayerData.STAT.PISTOL_FRAG, killer.Team, 1);
        }
        public int getWeaponType(EquipmentElement e){
            //0 is rifle, 1 is sniper, 2 is smgs, 3 pistols
            switch(e) {
                case EquipmentElement.AK47:
                    return 0;
                case EquipmentElement.M4A1:
                    return 0;
                case EquipmentElement.M4A4:
                    return 0;
                case EquipmentElement.AUG:
                    return 0;
                case EquipmentElement.SG556:
                    return 0;
                case EquipmentElement.UMP:
                    return 1;
                case EquipmentElement.MP7:
                    return 1;
                case EquipmentElement.MP9:
                    return 1;
                case EquipmentElement.Mac10:
                    return 1;
                case EquipmentElement.P90:
                    return 1;
                case EquipmentElement.Nova:
                    return 1;
                case EquipmentElement.SawedOff:
                    return 1;
                case EquipmentElement.Swag7:
                    return 1;
                case EquipmentElement.AWP:
                    return 2;
                case EquipmentElement.Scout:
                    return 2;
                case EquipmentElement.G3SG1:
                    return 2;
                case EquipmentElement.Scar20:
                    return 2;
                case EquipmentElement.CZ:
                    return 3;
                case EquipmentElement.Deagle:
                    return 3;
                case EquipmentElement.Glock:
                    return 3;
                case EquipmentElement.FiveSeven:
                    return 3;
                case EquipmentElement.P250:
                    return 3;
                case EquipmentElement.DualBarettas:
                    return 3;
                case EquipmentElement.P2000:
                    return 3;
                case EquipmentElement.USP:
                    return 3;
                case EquipmentElement.Tec9:
                    return 3;
            }
            return -1;
        }
    }
}
