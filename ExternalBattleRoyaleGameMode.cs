// ExternalCoolGame.cs
using RainMeadow;

using System;
using System.Collections.Generic;
using RWCustom;
using MoreSlugcats;
using System.Linq;
using UnityEngine;
using MonoMod.RuntimeDetour;

//BUGS
/*
Player slugcat color isn't applied. Players always use the color of the slugcat default
Spectating uses the room the players'  body is in.
*/

//TODO
/*
Make sure to take exclusive rooms into account. They should be ignored/used as areablocker if its the wrong scug.

*/

namespace MeadowBattleRoyale
{
    public class ExternalBattleRoyaleGameMode : OnlineGameMode
    {

        public BattleRoyaleData battleRoyaleData;
        public SlugcatCustomization avatarSettings;
        int abstractNode = 0;
        string myCurrentRoom = "";
        bool amIInRoomWithIncomingRain = false;
        WorldCoordinate spawnCoord;
        public List<RoomInfo> roomInfos; //Only the host keeps track of this

        private Color dangerShortCutColor = new Color(1f, 0f, 0f);
        private Color mediumShortCutColor = new Color(1f, 1f, 0f);
        private Color safeShortCutColor = new Color(0f, 1f, 0f);




        public void WorldSessionWorldStateReadTo(Action<WorldSession.WorldState, OnlineResource> orig, WorldSession.WorldState self, OnlineResource resource)
        {

            try
            {
                orig(self, resource);
                RainWorldGame game = Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
                CheckForRoomRain(myCurrentRoom, game);
            }
            catch (Exception ex)
            {
                BattleRoyale.Logger.LogError($"Error in ReadTo hook: {ex.Message}");
            }
        }

        public ExternalBattleRoyaleGameMode(Lobby lobby) : base(lobby)
        {
            avatarSettings = new SlugcatCustomization() { nickname = OnlineManager.mePlayer.id.name };
            avatarSettings.eyeColor = RainMeadow.RainMeadow.rainMeadowOptions.EyeColor.Value;
            avatarSettings.bodyColor = RainMeadow.RainMeadow.rainMeadowOptions.BodyColor.Value;
            avatarSettings.nickname = OnlineManager.mePlayer.id.name;


            On.OverWorld.LoadFirstWorld += OnOverworldFirstLoad;
            On.RegionGate.customKarmaGateRequirements += OnRegionGateCustomKarmaGateRequirements;
            On.GameSession.AddPlayer += OnGameSessionAddPlayer;
            //On.Player.Die += OnPlayerDie;
            On.Creature.Die += Creature_Die;
            On.HUD.TextPrompt.UpdateGameOverString += TextPrompt_UpdateGameOverString;
            On.HUD.TextPrompt.Update += TextPrompt_Update;
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
            On.ShortcutGraphics.Draw += DrawShortcutGraphics;

            new Hook(
                typeof(WorldSession.WorldState).GetMethod(nameof(WorldSession.WorldState.ReadTo)),
                new Action<Action<WorldSession.WorldState, OnlineResource>, WorldSession.WorldState, OnlineResource>(WorldSessionWorldStateReadTo)
            );
        }

        private void DrawShortcutGraphics(On.ShortcutGraphics.orig_Draw orig, ShortcutGraphics self, float timeStacker, Vector2 camPos)
        {
            orig(self, timeStacker, camPos);
            for (int i = 0; i < self.room.shortcuts.Length; i++)
            {
                if (self.room.shortcuts[i].shortCutType != ShortcutData.Type.RoomExit) continue;
                if (i >= self.entranceSprites.GetLength(0)) continue; // Prevent crash

                int destNode = self.room.shortcuts[i].destNode;

                // make sure destNode is valid
                if (destNode < 0 || destNode >= self.room.abstractRoom.connections.Length) continue;

                int connectedRoomIndex = self.room.abstractRoom.connections[destNode];
                AbstractRoom connectedRoom = self.room.world.GetAbstractRoom(connectedRoomIndex);

                if (connectedRoom == null) continue; // extra safety
                string connectedRoomName = connectedRoom.name;

                if (self.entranceSprites[i, 0] != null)
                {
                    if (battleRoyaleData.roomsWithRain.Contains(connectedRoomName))
                    {
                        self.entranceSprites[i, 0].color = dangerShortCutColor;
                    }
                    else if (battleRoyaleData.roomsWithoutRain.Contains(connectedRoomName))
                    {
                        self.entranceSprites[i, 0].color = safeShortCutColor;
                    }
                    else if (battleRoyaleData.roomsWithIncomingRain.Contains(connectedRoomName))
                    {
                        self.entranceSprites[i, 0].color = CalculateShortcutDanger(self.room.world);
                    }
                    else
                    {
                        self.entranceSprites[i, 0].color = new Color(1f, 1f, 1f);
                    }
                }
            }
        }

        public Color CalculateShortcutDanger(World world)
        {
            float dangerPercent = (float)battleRoyaleData.stormTimer / battleRoyaleData.stormTimerMax;
            return Color.Lerp(safeShortCutColor, dangerShortCutColor, dangerPercent);
        }
        private void Creature_Die(On.Creature.orig_Die orig, Creature crit)
        {
            orig(crit);
            if (!crit.dead) // Prevent death messages from firing 987343 times.
            {
                BattleRoyale.Logger.LogMessage($"{battleRoyaleData.playersLeft.Count} players left before removing self");
                if (true)//battleRoyaleData.playersLeft.Remove(OnlineManager.mePlayer))
                {
                    BattleRoyale.Logger.LogMessage($"{battleRoyaleData.playersLeft.Count} players left alive");
                    if (battleRoyaleData.playersLeft.Count <= 1)
                    {
                        battleRoyaleData.battleRoyaleState = BattleRoyaleStatus.PlayerHasWon;
                        battleRoyaleData.stormTimer = 0;
                        string lastPlayer = battleRoyaleData.playersLeft[0].id.name;
                        BattleRoyale.Logger.LogMessage($"Last player left is {lastPlayer}");



                        foreach (OnlinePlayer player in OnlineManager.players)
                        {
                            try
                            {
                                player.InvokeOnceRPC(NotifyThatPlayerWon, lastPlayer, battleRoyaleData.playersLeft[0]);
                            }
                            catch (Exception ex)
                            {
                                BattleRoyale.Logger.LogError($"Something went wrong trying to invoke: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        foreach (OnlinePlayer player in OnlineManager.players)
                        {
                            try
                            {
                                player.InvokeOnceRPC(NotifyThatPlayerDied, battleRoyaleData.playersLeft.Count, OnlineManager.mePlayer.ToString());
                            }
                            catch (Exception ex)
                            {
                                BattleRoyale.Logger.LogError($"Something went wrong trying to invoke: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }
        private void TextPrompt_UpdateGameOverString(On.HUD.TextPrompt.orig_UpdateGameOverString orig, HUD.TextPrompt self, Options.ControlSetup.Preset controllerType)
        {
            self.gameOverString = "Waiting for the game to be over, press " + RainMeadow.RainMeadow.rainMeadowOptions.SpectatorKey.Value + " to spectate.";
        }
        private void TextPrompt_Update(On.HUD.TextPrompt.orig_Update orig, HUD.TextPrompt self)
        {
            orig(self);
            if (OnlineManager.lobby != null && self.currentlyShowing == HUD.TextPrompt.InfoID.GameOver)
            {
                self.restartNotAllowed = 1; // block from GoToDeathScreen

                bool touchedInput = false;
                for (int j = 0; j < self.hud.rainWorld.options.controls.Length; j++)
                {
                    touchedInput = (self.hud.rainWorld.options.controls[j].gamePad || !self.defaultMapControls[j]) ? (touchedInput || self.hud.rainWorld.options.controls[j].GetButton(5) || RWInput.CheckPauseButton(0, inMenu: false)) : (touchedInput || self.hud.rainWorld.options.controls[j].GetButton(11));
                }
                if (touchedInput)
                {
                    self.gameOverMode = false;
                }
            }
        }
        private void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);
            self.AddPart(new OnlineHUD(self, cam, this));
            self.AddPart(new SpectatorHud(self, cam));
            self.AddPart(new Pointing(self));
            
            if (MatchmakingManager.currentInstance.canSendChatMessages) self.AddPart(new ChatHud(self, cam));
        }
        private void OnRegionGateCustomKarmaGateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, RegionGate self)
        {
            self.karmaRequirements[0] = RegionGate.GateRequirement.DemoLock;
            self.karmaRequirements[1] = RegionGate.GateRequirement.DemoLock;
        }
        private void OnOverworldFirstLoad(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
        {
            foreach (int key in battleRoyaleData.playerStartingRoomsKey)
            {
                if (OnlineManager.lobby.PlayerFromId((ushort)key).isMe)
                {
                    string myRoom = battleRoyaleData.playerStartingRoomsValue[battleRoyaleData.playerStartingRoomsKey.IndexOf(key)];
                    self.LoadWorld(battleRoyaleData.region, battleRoyaleData.slugcat, battleRoyaleData.worldstate, false);
                    self.FIRSTROOM = myRoom;

                    AbstractRoom spawnRoom = self.game.world.abstractRooms.First(x => x.name == myRoom);
                    List<int> entrancePipes = new();
                    for (int i4 = 0; i4 < spawnRoom.nodes.Length; i4++)
                    {

                        if (spawnRoom.nodes[i4].type == AbstractRoomNode.Type.Exit && i4 < spawnRoom.connections.Length && spawnRoom.connections[i4] > -1)
                        {
                            entrancePipes.Add(i4);
                        }
                    }
                    int pipe = entrancePipes[UnityEngine.Random.Range(0, entrancePipes.Count)];
                    abstractNode = pipe;
                    spawnRoom.RealizeRoom(self.game.world, self.game);
                    ShortcutMapper scMapper = new ShortcutMapper(spawnRoom.realizedRoom);
                    while (!scMapper.done)
                    {
                        scMapper.Update();
                    }
                    spawnCoord = spawnRoom.realizedRoom.LocalCoordinateOfNode(pipe);
                }
            }
        }
        private void OnGameSessionAddPlayer(On.GameSession.orig_AddPlayer orig, GameSession self, AbstractCreature player)
        {
            orig(self, player);
            foreach (AbstractCreature ply in self.Players)
            {
                ply.pos = spawnCoord;
                ply.pos.abstractNode = abstractNode;

                //Visual and auditory flair for the player spawning :3
                ply.Room.realizedRoom.PlaySound(SoundID.UI_Multiplayer_Game_Start);
                ply.Room.realizedRoom.AddObject(new GhostPing(ply.Room.realizedRoom));
                if (battleRoyaleData.region == "SH")
                {
                    (ply.realizedCreature as Player).glowing = true;
                }
            }
        }
        public override void Customize(Creature creature, OnlineCreature oc)
        {
            BattleRoyale.Logger.LogMessage("Trying to apply customization");
            BattleRoyale.Logger.LogMessage($"result is {oc.TryGetData<SlugcatCustomization>(out var custom)}");
            if (oc.TryGetData<SlugcatCustomization>(out var data))
            {
                BattleRoyale.Logger.LogMessage($"getting value {RainMeadow.RainMeadow.creatureCustomizations.GetValue(creature, (c) => data)}");
                RainMeadow.RainMeadow.creatureCustomizations.GetValue(creature, (c) => data);
            }
        }
        public override void ConfigureAvatar(OnlineCreature onlineCreature)
        {

        }
        public override ProcessManager.ProcessID MenuProcessId()
        {
            return new("BattleRoyaleMenu", true);
        }
        public override bool PlayersCanStack => false; //Disables players being able to stack
        public override bool PlayersCanHandhold => false; //Disables players being able to hold each other
        public override bool ShouldSpawnRoomItems(RainWorldGame game, RoomSession roomSession) //Allow items to spawn
        {
            return true;
        }
        public override bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            return false;
        }
        public override SlugcatStats.Name GetStorySessionPlayer(RainWorldGame self)
        {
            return battleRoyaleData.slugcat;
        }
        public override SlugcatStats.Name LoadWorldAs(RainWorldGame game) //Force the worldstate to be Survivor
        {
            return battleRoyaleData.slugcat; //Todo: fix
        }
        public override void ResourceAvailable(OnlineResource onlineResource)
        {
            base.ResourceAvailable(onlineResource);
            if (onlineResource is Lobby lobby)
            {
                this.battleRoyaleData = lobby.AddData(new BattleRoyaleData());
            }
        }
        public override void PlayerLeftLobby(OnlinePlayer player)
        {
            base.PlayerLeftLobby(player);
            battleRoyaleData.playersLeft.Remove(player);
        }
        public override void LobbyTick(uint tick)
        {
            base.LobbyTick(tick);
            if (lobby.isOwner)
            {
                if (battleRoyaleData.battleRoyaleState == BattleRoyaleStatus.GameNotStarted)
                {
                    return;
                }
                if (battleRoyaleData.battleRoyaleState == BattleRoyaleStatus.PlayerHasWon && battleRoyaleData.stormTimer == 20 * 5)
                {
                    battleRoyaleData.battleRoyaleState = BattleRoyaleStatus.GameNotStarted;
                    foreach (OnlinePlayer player in OnlineManager.players)
                    {
                        try
                        {
                            player.InvokeOnceRPC(SendBackToLobby);
                        }
                        catch (Exception ex)
                        {
                            BattleRoyale.Logger.LogError($"Something went wrong trying to invoke: {ex.Message}");
                        }
                    }
                }
                battleRoyaleData.stormTimer++;
                if (battleRoyaleData.battleRoyaleState == BattleRoyaleStatus.PeaceTime && battleRoyaleData.stormTimer > 0)
                {
                    battleRoyaleData.battleRoyaleState = BattleRoyaleStatus.Regular;
                }
                if (battleRoyaleData.stormTimer > battleRoyaleData.stormTimerMax)
                {
                    battleRoyaleData.stormTimer = 0;
                    foreach (string room in battleRoyaleData.roomsWithIncomingRain)
                    {
                        battleRoyaleData.roomsWithRain.Add(room);
                    }
                    battleRoyaleData.roomsWithIncomingRain.Clear();
                    int roomAmount = 0;
                    if (battleRoyaleData.roomsWithoutRain.Count - (int)Math.Ceiling((float)roomInfos.Count / 10) <= 1 && battleRoyaleData.battleRoyaleState == BattleRoyaleStatus.Regular)
                    {
                        if (battleRoyaleData.battleRoyaleState == BattleRoyaleStatus.OneRoomRemainingWait)
                        {
                            battleRoyaleData.battleRoyaleState = BattleRoyaleStatus.FinalBattle;
                            battleRoyaleData.stormTimer -= battleRoyaleData.finalBattleTimer;
                            roomAmount = battleRoyaleData.roomsWithoutRain.Count;
                        }
                        else
                        {
                            //Final room has been reached!
                            battleRoyaleData.battleRoyaleState = BattleRoyaleStatus.OneRoomRemainingWait;
                            roomAmount = battleRoyaleData.roomsWithoutRain.Count - 1;
                        }


                    }
                    else
                    {
                        roomAmount = (int)Math.Ceiling((float)roomInfos.Count / 10);
                    }
                    StartRainInRooms(roomAmount);

                }
            }


            foreach (OnlineEntity.EntityId playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo1 && opo1.isMine && opo1.apo is AbstractCreature ply)
                {
                    try
                    {
                        if (amIInRoomWithIncomingRain == true)
                        {
                            ply.Room.world.rainCycle.timer = CalculateRainProgress(ply.world.rainCycle.cycleLength);
                        }
                        if (ply.Room.name != myCurrentRoom)
                        {
                            myCurrentRoom = ply.Room.name;
                            ply.Room.realizedRoom.roomSettings.RainIntensity = 1f;
                            if (ply.Room.realizedRoom.roomSettings.DangerType == RoomRain.DangerType.None)
                            {
                                ply.Room.realizedRoom.roomSettings.DangerType = RoomRain.DangerType.Rain;
                                if (ply.Room.world.name == "SS")
                                {
                                    ply.Room.realizedRoom.roomSettings.DangerType = RoomRain.DangerType.Thunder;
                                    ElectricDeath death = new ElectricDeath(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.ElectricDeath, 1f, false), ply.Room.realizedRoom);
                                    ply.Room.realizedRoom.AddObject(death);

                                }
                                else if (ply.Room.world.name == "LC" && ModManager.Watcher)
                                {
                                    ply.Room.realizedRoom.roomSettings.DangerType = Watcher.WatcherEnums.WatcherDangerType.Sandstorm;
                                }
                            }
                        }
                        string roomName = ply.Room.name;
                        if (ply.realizedCreature.dead)
                        {
                            roomName = ply.world.game.cameras[0].room.abstractRoom.name;
                        }
                        CheckForRoomRain(roomName, ply.world.game);
                    }
                    catch (Exception ex)
                    {
                        BattleRoyale.Logger.LogMessage("Should only trigger a few times in round start: " + ex.Message);
                    }
                }
            }
        }
        public override void PostGameStart(RainWorldGame self)
        {
            BattleRoyale.Logger.LogInfo("Game has been started!");
            myCurrentRoom = "";
            amIInRoomWithIncomingRain = false;
            self.globalRain.ResetRain();

            if (lobby.isOwner)
            {
                battleRoyaleData.battleRoyaleState = BattleRoyaleStatus.PeaceTime;
                battleRoyaleData.stormTimer = 0;
                battleRoyaleData.roomsWithRain = new();
                battleRoyaleData.roomsWithoutRain = new();
                battleRoyaleData.roomsWithIncomingRain = new();
                battleRoyaleData.stormTimer -= battleRoyaleData.timeBeforeStormsStart; //Subtract from the timer to get extra time
                foreach (RoomInfo info in roomInfos) //Add all blocker rooms to the global rain list
                {
                    if (info.DistanceScore == int.MaxValue)
                    {
                        battleRoyaleData.roomsWithRain.Add(info.Name);
                    }
                    else
                    {
                        battleRoyaleData.roomsWithoutRain.Add(info.Name);
                    }
                }
                StartRainInRooms((int)Math.Ceiling((float)roomInfos.Count / 10), self);
            }
            base.PostGameStart(self);

        }
        public void StartRainInRooms(int count, RainWorldGame game = null)
        {
            List<RoomInfo> eligibleRooms = roomInfos
                .Where(r => battleRoyaleData.roomsWithoutRain.Contains(r.Name))
                .OrderByDescending(r => r.DistanceScore)
                .ToList();

            List<RoomInfo> furthestRooms = eligibleRooms.Take(count).ToList();
            string debugDisplay = "";
            foreach (RoomInfo room in furthestRooms)
            {
                battleRoyaleData.roomsWithIncomingRain.Add(room.Name);
                battleRoyaleData.roomsWithoutRain.Remove(room.Name);
                debugDisplay += room.Name + ", ";
            }
        }

        public void CheckForRoomRain(string room, RainWorldGame game)
        {

            if (battleRoyaleData.roomsWithoutRain.Contains(room))
            {
                amIInRoomWithIncomingRain = false;
                if (game.globalRain.deathRain != null)
                {
                    game.globalRain.ResetRain();
                }
                game.world.rainCycle.timer = -1;
                return;
            }
            else if (battleRoyaleData.roomsWithRain.Contains(room))
            {
                amIInRoomWithIncomingRain = false;
                game.world.rainCycle.timer = 999999999;
                if (game.globalRain.deathRain == null)
                {
                    game.globalRain.InitDeathRain();
                }
                game.globalRain.Intensity = 1f;
                game.globalRain.deathRain.deathRainMode = GlobalRain.DeathRain.DeathRainMode.FinalBuildUp;
                game.globalRain.deathRain.progression = float.MaxValue;

                return;
            }
            else if (battleRoyaleData.roomsWithIncomingRain.Contains(room))
            {
                amIInRoomWithIncomingRain = true;
                if (game.globalRain.deathRain != null)
                {
                    game.globalRain.ResetRain();
                }
                game.world.rainCycle.timer = CalculateRainProgress(game.world.rainCycle.cycleLength);
                return;
            }

        }

        private int CalculateRainProgress(int cycleLength)
        {
            int timerValue = (int)((float)battleRoyaleData.stormTimer / battleRoyaleData.stormTimerMax * cycleLength);
            return timerValue;
        }

        public override void GameShutDown(RainWorldGame game)
        {
            base.GameShutDown(game);
            if (lobby.isOwner)
            {
                battleRoyaleData.playersLeft.Clear();
                lobby.NewVersion();
            }
        }

        [RPCMethod]
        public static void SendBackToLobby()
        {
            ProcessManager manager = Custom.rainWorld.processManager;
            manager.arenaSitting = null;
            manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            manager.RequestMainProcessSwitch(BattleRoyale.BattleRoyaleMenu);
        }

        [RPCMethod]
        public static void NotifyThatPlayerDied(int playersLeft, string player)
        {
            BattleRoyale.Logger.LogMessage($"Player {player} has died, leaving only {playersLeft} remaining");
            if (playersLeft == 2)
            {
                ChatLogManager.LogSystemMessage($"Only 2 players remain!");
            }
            else
            {
                ChatLogManager.LogSystemMessage($"{playersLeft} players left!");
            }

            //https://github.com/henpemaz/Rain-Meadow/blob/1b67ebbbce29f2d3a1de2cff9f2bf416da11d506/OnlineUIComponents/DeathMessage.cs#L253 to add to the kill feed
            //^ also has the player death events right below it.
        }

        [RPCMethod]
        public static void NotifyThatPlayerWon(string playerName, OnlinePlayer onlinePlayer)
        {
            BattleRoyale.Logger.LogMessage($"Player {playerName} has won!");
            ChatLogManager.LogSystemMessage($"{playerName} won!");
            AbstractCreature player = null;
            foreach (OnlineEntity.EntityId playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo1 && opo1.apo is AbstractCreature ac)
                {
                    if (ac.GetOnlineCreature().owner == onlinePlayer)
                    {
                        player = ac;
                        break;
                    }
                }
            }
            if (player == null)
            {
                BattleRoyale.Logger.LogMessage($"Player reference was null, so flair was disabled.");
                return;
            }

            if (player.realizedCreature == null)
            {
                BattleRoyale.Logger.LogMessage($"realizedCreature was null, can't apply flair.");
                return;
            }
            var gfx = player.realizedCreature.graphicsModule as PlayerGraphics;
            if (gfx == null)
            {
                BattleRoyale.Logger.LogMessage($"Graphics module was not PlayerGraphics or was null.");
                return;
            }


            // Fully darken the player immediately
            //gfx.darkenFactor = 1f;

            // Big spark burst for dramatic flair
            for (int j = 0; j < 60; j++)
            {
                float value = UnityEngine.Random.Range(0f, 1f);
                Spark spark = new Spark(
                    player.realizedCreature.mainBodyChunk.pos,
                    Custom.RNV() * value * 5f,
                    new Color(1f, 1f, 1f),
                    null,
                    10 + (int)((1f - value) * 60f),
                    36 + (int)((1f - value) * 200f)
                );
                spark.gravity = 0f;
                player.realizedCreature.room.AddObject(spark);
            }
            player.realizedCreature.room.AddObject(new ShockWave(player.realizedCreature.mainBodyChunk.pos, 500f, 0.75f, 18));
            player.realizedCreature.room.AddObject(new Explosion.ExplosionLight(player.realizedCreature.mainBodyChunk.pos, 320f, 1f, 5, Color.white));
            player.realizedCreature.room.AddObject(new GhostPing(player.realizedCreature.room));

            player.world.game.cameras[0].hud.AddPart(new PlayerWinDisplay(playerName,player.world.game.cameras[0].hud.fContainers[0],player.world.game.cameras[0].hud));
            //self.AddPart(new ArenaPrepTimer(self, self.fContainers[0], arena, session));
            //player.realizedCreature.room.gravity = 0f;
        }
    }
}