using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow;
using RWCustom;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Unity.Mathematics;

namespace MeadowBattleRoyale
{
    internal class BattleRoyaleMenu : CustomLobbyMenu
    {
        Lobby lobby;
        ExternalBattleRoyaleGameMode gamemode;
        SlugcatCustomization customization;
        SlugcatCustomizationSelectorNoScug customizationHolder;
        SimplerButton playButton;
        OpUpdown setupTime;
        OpUpdown finalBattleTime;
        OpUpdown roomRainWaitTime;
        OpComboBox2 slugcatSelector;
        OpComboBox2 regionSelector;
        RainEffect rainEffect;
        EventfulHoldButton startButton;

        private string GetTitleFileName(bool isShadow)
        {
            string fileName = isShadow ? "title_shadow" : "title";

            return fileName;
        }
        public BattleRoyaleMenu(ProcessManager manager) : base(manager, BattleRoyale.BattleRoyaleMenu)
        {

            this.scene.AddIllustration(new MenuIllustration(this, this.scene, "illustrations", GetTitleFileName(true), new Vector2(-2.99f, 265.01f), true, false));
            this.scene.AddIllustration(new MenuIllustration(this, this.scene, "illustrations", GetTitleFileName(false), new Vector2(-2.99f, 265.01f), true, false));
            this.scene.flatIllustrations[this.scene.flatIllustrations.Count - 1].sprite.shader = this.manager.rainWorld.Shaders["MenuText"];
            this.rainEffect = new RainEffect(this, this.scene);
            mainPage.subObjects.Add(rainEffect);
            this.rainEffect.rainFade = 0.3f;

            FSprite sprite = new FSprite("pixel") { x = 658, y = 450, anchorY = 0, scaleX = 222, scaleY = 2,color = MenuRGB(MenuColors.MediumGrey) };
            mainPage.Container.AddChild(sprite);
            sprites.Add(sprite);
            sprite = new FSprite("pixel") { x = 658, y = 400, anchorY = 0, scaleX = 222, scaleY = 2,color = MenuRGB(MenuColors.MediumGrey) };
            mainPage.Container.AddChild(sprite);
            sprites.Add(sprite);

            // customization
            this.lobby = OnlineManager.lobby;
            this.gamemode = (ExternalBattleRoyaleGameMode)lobby.gameMode;
            this.customization = gamemode.avatarSettings;

            this.customizationHolder = new SlugcatCustomizationSelectorNoScug(this, this.mainPage, new Vector2(540, 520), customization);
            mainPage.subObjects.Add(this.customizationHolder);


            if (lobby.isOwner) //Only the owner of the lobby sees these
            {
                Vector2 optionsPosition = new Vector2(540, 400);
                Vector2 currentOffset = new Vector2(0, 0);
                int objectPaddingVertical = 30;
                int objectPaddingHorizontal = 100;
                Vector2 calculatedPosition;
                Vector2 calculatedPositionHorizontal;

                // playButton = new SimplerButton(this, mainPage, Translate("PLAY!"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
                // playButton.OnClick += Play;
                // mainPage.subObjects.Add(playButton);

                this.startButton = new EventfulHoldButton(this, this.mainPage, base.Translate("START GAME"), new Vector2(683f, 85f), 40f);
                this.startButton.OnClick += (_) => { Play(); };
                this.mainPage.subObjects.Add(this.startButton);

                calculatedPosition = optionsPosition + currentOffset;
                calculatedPositionHorizontal = new Vector2(calculatedPosition.x + objectPaddingHorizontal * 2, calculatedPosition.y);
                currentOffset.y -= objectPaddingVertical;
                //
                mainPage.subObjects.Add(new MenuLabel(this, mainPage, "Pre-Storm time (seconds)", calculatedPosition, new Vector2(120, 20), false));
                this.setupTime = new OpUpdown(
                new Configurable<int>(gamemode.battleRoyaleData.timeBeforeStormsStart / 20, new ConfigAcceptableRange<int>(0, 600)), calculatedPositionHorizontal, 120);
                new UIelementWrapper(tabWrapper, this.setupTime);
                setupTime._lastArrX = setupTime._arrX;
                setupTime.OnValueChanged += SetupTime_OnValueChanged;

                calculatedPosition = optionsPosition + currentOffset;
                calculatedPositionHorizontal = new Vector2(calculatedPosition.x + objectPaddingHorizontal * 2, calculatedPosition.y);
                currentOffset.y -= objectPaddingVertical;
                //
                mainPage.subObjects.Add(new MenuLabel(this, mainPage, "Final battle length (seconds)", calculatedPosition, new Vector2(120, 20), false));
                this.finalBattleTime = new OpUpdown(
                new Configurable<int>(gamemode.battleRoyaleData.finalBattleTimer / 20, new ConfigAcceptableRange<int>(0, 600)), calculatedPositionHorizontal, 120);
                new UIelementWrapper(tabWrapper, this.finalBattleTime);
                finalBattleTime._lastArrX = finalBattleTime._arrX;
                finalBattleTime.OnValueChanged += FinalBattleTime_OnValueChanged;

                calculatedPosition = optionsPosition + currentOffset;
                calculatedPositionHorizontal = new Vector2(calculatedPosition.x + objectPaddingHorizontal * 2, calculatedPosition.y);
                currentOffset.y -= objectPaddingVertical;
                //
                mainPage.subObjects.Add(new MenuLabel(this, mainPage, "Room rain wait (seconds)", calculatedPosition, new Vector2(120, 20), false));
                this.roomRainWaitTime = new OpUpdown(
                new Configurable<int>(gamemode.battleRoyaleData.stormTimerMax / 20, new ConfigAcceptableRange<int>(0, 600)), calculatedPositionHorizontal, 120);
                new UIelementWrapper(tabWrapper, this.roomRainWaitTime);
                roomRainWaitTime._lastArrX = roomRainWaitTime._arrX;
                roomRainWaitTime.OnValueChanged += RoomRainWaitTime_OnValueChanged;


                currentOffset.y -= objectPaddingVertical;
                calculatedPosition = optionsPosition + currentOffset;
                calculatedPositionHorizontal = new Vector2(calculatedPosition.x + objectPaddingHorizontal, calculatedPosition.y);
                currentOffset.y -= objectPaddingVertical;
                //
                mainPage.subObjects.Add(new MenuLabel(this, mainPage, "Slugcat", calculatedPosition, new Vector2(120, 20), false));
                this.slugcatSelector = new OpComboBox2(
                new Configurable<SlugcatStats.Name>(
                    gamemode.battleRoyaleData.slugcat ?? SlugcatStats.Name.White), calculatedPositionHorizontal, 160, Menu.Remix.MixedUI.OpResourceSelector.GetEnumNames(null, typeof(SlugcatStats.Name))
                    .Select(li =>
                    {
                        li.displayName = this.Translate(li.displayName);
                        return li;
                    }).ToList());
                new UIelementWrapper(tabWrapper, this.slugcatSelector);
                slugcatSelector.OnValueChanged += SlugcatSelector_OnValueChanged;

                //Region[] regions = Region.LoadAllRegions(SlugcatStats.Timeline.White);
                var allRegions = new List<Region>();
                foreach (string name in SlugcatStats.Timeline.values.entries)
                {
                    SlugcatStats.Timeline timeline = new SlugcatStats.Timeline(name);

                    try
                    {
                        Region[] currRegions = Region.LoadAllRegions(timeline);
                        if (currRegions != null)
                            allRegions.AddRange(currRegions);
                    }
                    catch (Exception ex)
                    {
                        BattleRoyale.Logger.LogError($"Could not load regions for {timeline}: {ex.Message}");
                    }
                }

                // If you want to remove duplicates:
                var regions = allRegions
                    .GroupBy(r => r.name)
                    .Select(g => g.First())
                    .ToArray();

                string defaultRegion = "SU";
                List<Menu.Remix.MixedUI.ListItem> regionList = new List<ListItem>();

                foreach (Region region in regions)
                {
                    string fullName = Region.GetRegionFullName(region.name, gamemode.battleRoyaleData.slugcat);
                    regionList.Add(new Menu.Remix.MixedUI.ListItem(region.name, fullName));
                    if (region.name == defaultRegion && gamemode.battleRoyaleData.region == null)
                    {
                        gamemode.battleRoyaleData.region = region.name; //Set the default region to SU
                    }
                }

                calculatedPosition = optionsPosition + currentOffset;
                calculatedPositionHorizontal = new Vector2(calculatedPosition.x + objectPaddingHorizontal, calculatedPosition.y);
                currentOffset.y -= objectPaddingVertical;
                //
                var selectedRegion = new Configurable<string>(
    gamemode.battleRoyaleData.region ?? defaultRegion);

                mainPage.subObjects.Add(new MenuLabel(this, mainPage, "Region", calculatedPosition, new Vector2(120, 20), false));
                this.regionSelector = new OpComboBox2(selectedRegion, calculatedPositionHorizontal, 160, regionList);
                new UIelementWrapper(tabWrapper, this.regionSelector);
                regionSelector.OnValueChanged += RegionSelector_OnValueChanged;
            }
        }

        private void RoomRainWaitTime_OnValueChanged(UIconfig config, string value, string oldValue)
        {
            gamemode.battleRoyaleData.stormTimerMax = (ushort)Mathf.Clamp(finalBattleTime.valueInt, 0, 600)*20;
        }

        private void FinalBattleTime_OnValueChanged(UIconfig config, string value, string oldValue)
        {
            gamemode.battleRoyaleData.finalBattleTimer = (ushort)Mathf.Clamp(finalBattleTime.valueInt, 0, 600)*20;
        }

        private void SetupTime_OnValueChanged(UIconfig config, string value, string oldValue)
        {
            gamemode.battleRoyaleData.timeBeforeStormsStart = (ushort)Mathf.Clamp(setupTime.valueInt, 0, 600)*20;
        }

        private void SlugcatSelector_OnValueChanged(UIconfig config, string value, string oldValue)
        {
            BattleRoyale.Logger.LogInfo("New slugcat selected: " + value);
            gamemode.battleRoyaleData.slugcat = new SlugcatStats.Name(value);
        }

        private void RegionSelector_OnValueChanged(UIconfig config, string value, string oldValue)
        {
            BattleRoyale.Logger.LogInfo("New region selected: " + value);
            gamemode.battleRoyaleData.region = value;

        }

        public override void Update()
        {
            base.Update();

            if (this.rainEffect != null)
            {
                this.rainEffect.rainFade = Mathf.Min(0.3f, this.rainEffect.rainFade + 0.006f);
            }
        }

        private void Play()
        {
            if (lobby.isOwner) //Should always be true since the only player that can see the start button is the host but just incase
            {
                List<RoomInfo> regionRooms = GetRegionRooms(gamemode.battleRoyaleData.region);
                //Read the text files that contain:
                //royale/<region>_stormcenters.txt
                //royale/<region>_areablockers.txt
                string stormCentersTxtPath = AssetManager.ResolveFilePath("royale/" + gamemode.battleRoyaleData.region + "_stormcenters.txt");
                string areaBlockersTxtPath = AssetManager.ResolveFilePath("royale/" + gamemode.battleRoyaleData.region + "_areablockers.txt");
                List<string> stormCenters = ParseStormCenters(stormCentersTxtPath);
                List<string> areaBlockers = ParseAreaBlockers(areaBlockersTxtPath);

                string chosenCenter = "";
                if (stormCenters.Count == 0)
                {//If there are no predetermined storm centers then pick a random one
                    chosenCenter = regionRooms[UnityEngine.Random.Range(0, regionRooms.Count)].Name;
                }
                else
                {
                    chosenCenter = stormCenters[UnityEngine.Random.Range(0, stormCenters.Count)];
                }
                //Generate the floodfill map for the region
                regionRooms = CalculateRoomDistanceFromCenter(regionRooms, chosenCenter, areaBlockers);
                gamemode.battleRoyaleData.playerStartingRoomsKey.Clear();
                gamemode.battleRoyaleData.playerStartingRoomsValue.Clear();
                for (int i = regionRooms.Count - 1; i > 0; i--) //Shuffles the rooms
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    RoomInfo temp = regionRooms[i];
                    regionRooms[i] = regionRooms[j];
                    regionRooms[j] = temp;
                }

                gamemode.roomInfos = regionRooms;

                Regex shelterPattern = new(@"^[A-Z]+_S\d+$", RegexOptions.IgnoreCase);

                // Don't modify regionRooms directly
                List<RoomInfo> eligibleRooms = regionRooms
                    .Where(r =>
                        !r.Name.ToUpper().Contains("GATE_") &&
                        !shelterPattern.IsMatch(r.Name))
                    .OrderByDescending(r => r.DistanceScore)
                    .ToList();

                int amountOfPlayers = OnlineManager.players.Count;
                List<RoomInfo> furthestRooms = eligibleRooms.Take(amountOfPlayers).ToList();

                // Shuffle the list
                for (int i = furthestRooms.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    RoomInfo temp = furthestRooms[i];
                    furthestRooms[i] = furthestRooms[j];
                    furthestRooms[j] = temp;
                }
                gamemode.battleRoyaleData.playersLeft.Clear();
                foreach (OnlinePlayer player in OnlineManager.players)
                {
                    RoomInfo chosenRoom = furthestRooms.Pop();
                    gamemode.battleRoyaleData.playerStartingRoomsKey.Add((int)player.inLobbyId);
                    gamemode.battleRoyaleData.playerStartingRoomsValue.Add(chosenRoom.Name);
                    try
                    {
                        gamemode.battleRoyaleData.playersLeft.Add(player);
                        player.InvokeOnceRPC(GetPutIntoGame, gamemode.battleRoyaleData.slugcat);
                    }
                    catch (Exception ex)
                    {
                        BattleRoyale.Logger.LogError($"Something went wrong trying to invoke: {ex.Message}");
                    }

                }
            }
        }

        

        [RPCMethod]
        public static void GetPutIntoGame(SlugcatStats.Name slugcat)
        {
            ProcessManager manager = Custom.rainWorld.processManager;
            manager.arenaSitting = null;
            manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat = slugcat;
            manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        private List<string> ParseStormCenters(string stormCentersTxtPath)
        {
            List<string> rooms = new();
            try
            {
                if (!File.Exists(stormCentersTxtPath))
                {
                    return rooms;
                }
                string[] roomLines = File.ReadAllLines(stormCentersTxtPath)[0].Split(',');
                foreach (string room in roomLines)
                {
                    rooms.Add(room.Trim());
                }
                return rooms;
            }
            catch
            {
                return rooms;
            }
        }

        private List<string> ParseAreaBlockers(string areaBlockersTxtPath)
        {
            List<string> rooms = new();
            try
            {
                if (!File.Exists(areaBlockersTxtPath))
                {
                    return rooms;
                }
                string[] roomLines = File.ReadAllLines(areaBlockersTxtPath)[0].Split(',');
                foreach (string room in roomLines)
                {
                    rooms.Add(room.Trim());
                }
                return rooms;
            }
            catch
            {
                return rooms;
            }
            
        }
        public List<RoomInfo> GetRegionRooms(string region)
        {
            BattleRoyale.Logger.LogInfo($"Getting rooms for region {region}");
            List<RoomInfo> roomList = new List<RoomInfo>();


            string filePath = AssetManager.ResolveFilePath("world/" + region + "/world_" + region + ".txt");
            List<string> rooms = ParseWorldFile(filePath, region);
            foreach (string room in rooms)
            {
                RoomInfo info = new();
                info.Name = room;
                info.Region = region;
                roomList.Add(info);
            }
            string worldPath = AssetManager.ResolveFilePath("world/" + region + "/world_" + region + ".txt");
            if (File.Exists(worldPath))
            {

                //TODO: Go through the conditional links too, to make sure that the rooms are actually accessible as the current worldstate
                string[] worldFile = File.ReadAllLines(worldPath);
                bool currentlyReadingRooms = false;
                for (int l = 0; l < worldFile.Length; l++)
                {
                    if (worldFile[l] == "ROOMS")
                    {
                        currentlyReadingRooms = true;
                        continue;
                    }
                    if (worldFile[l] == "END ROOMS")
                    {
                        currentlyReadingRooms = false;
                        continue;
                    }
                    if (!currentlyReadingRooms)
                    {
                        continue;
                    }
                    //string[] mapFileStrings = Regex.Split(Custom.ValidateSpacedDelimiter(mapFile[l], ":"), ": ");
                    string[] connectionString = worldFile[l].Split(' ');
                    foreach (RoomInfo info in roomList)
                    {
                        if (!(info.Name == connectionString[0])) //Check to see if the string we're looking at is the same as this room
                        {
                            continue;
                        }
                        //connectionString[0] is the room name
                        //connectionString[1] should be a :
                        //connectionString[2+] should be all the remaining rooms
                        for (int i = 2; i < connectionString.Length; i++)
                        {
                            string connectionName = connectionString[i].Trim(',');
                            if (!DiscardConnection(connectionName))
                            {
                                info.ConnectedRooms.Add(connectionName);
                            }

                        }
                    }
                }
            }
            return roomList;
        }

        public List<RoomInfo> CalculateRoomDistanceFromCenter(List<RoomInfo> inputList, string centerRoom, List<string> areaBlockers)
        {
            RoomInfo centerRoomInfo = inputList.Find(r => r.Name == centerRoom);
            if (centerRoomInfo == null)
            {
                 return null;
            }
               

            centerRoomInfo.DistanceScore = 0;

            List<RoomInfo> returnList = new();
            Queue<RoomInfo> queue = new();
            HashSet<string> inQueue = new();

            queue.Enqueue(centerRoomInfo);
            inQueue.Add(centerRoomInfo.Name);

            while (queue.Count > 0)
            {
                RoomInfo currentRoom = queue.Dequeue();
                inQueue.Remove(currentRoom.Name);

                // Don't process blockers (just skip them entirely)
                if (areaBlockers != null && areaBlockers.Contains(currentRoom.Name))
                    continue;

                returnList.Add(currentRoom);

                foreach (string connectedRoomName in currentRoom.ConnectedRooms)
                {
                    if (areaBlockers != null && areaBlockers.Contains(connectedRoomName))
                        continue;

                    RoomInfo neighbor = inputList.Find(r => r.Name == connectedRoomName);
                    if (neighbor != null)
                    {
                        int newScore = currentRoom.DistanceScore + 1;
                        if (newScore < neighbor.DistanceScore)
                        {
                            neighbor.DistanceScore = newScore;

                            if (!inQueue.Contains(neighbor.Name))
                            {
                                queue.Enqueue(neighbor);
                                inQueue.Add(neighbor.Name);
                            }
                        }
                    }
                }
            }

            return returnList;
        }
        public bool DiscardConnection(string connectionName)
        {
            switch (connectionName)
            {
                default: return false;
                case "GATE": return true;
                case ":": return true;
                case "DISCONNECTED": return true;
                case "SHELTER": return true;
                case "SWARMROOM": return true;
                case "SCAVTRADER": return true;
                case "PERF_HEAVY": return true;
                case "SCAVOUTPOST": return true;
            }
        }


        public List<string> ParseWorldFile(string path, string region)
        {
            List<string> roomInfo = new List<string>();
            if (File.Exists(path))
            {
                bool roomSection = false;
                string[] worldFile = File.ReadAllLines(path);
                for (int i = 0; i < worldFile.Length && !(worldFile[i] == "END ROOMS"); i++)
                {
                    if (roomSection && worldFile[i].Length > 0 && worldFile[i][0] != ' ' && worldFile[i][0] != '/')
                    {
                        string name = "";
                        string[] roomLine = Regex.Split(worldFile[i], " : ");
                        if (roomLine != null && (roomLine[0].Contains(region + "_") || roomLine[0].Contains("GATE_")))
                        {
                            if (roomLine[0].Contains("}"))
                            {
                                name = Regex.Split(roomLine[0], "\\}")[1];
                            }
                            else if (roomLine[0].Contains(")"))
                            {
                                name = Regex.Split(roomLine[0], "\\)")[1];
                            }
                            else
                            {
                                name = roomLine[0];
                            }

                            if (!roomInfo.Contains(name))
                            {
                                roomInfo.Add(name);
                            }
                        }
                    }
                    if (worldFile[i] == "ROOMS")
                    {
                        roomSection = true;
                    }
                }
            }
            return roomInfo;
        }
    }
    public class RoomInfo
        {
            public string Name { get; set; }
            public string Region { get; set; }
            public List<string> ConnectedRooms = new();
            public int DistanceScore = int.MaxValue;
        }
}