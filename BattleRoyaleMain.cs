using BepInEx;
using IL;
using RainMeadow;
using System;
using System.Collections.Generic;
using RWCustom;
using MoreSlugcats;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Permissions;
using UnityEngine;
using MonoMod.RuntimeDetour;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace MeadowBattleRoyale
{
    [BepInPlugin("Mirarge.BattleRoyale", "Meadow Royale", "0.1.0")]
    public partial class BattleRoyale : BaseUnityPlugin
    {
        public static BattleRoyale instance;
        private bool init;
        private bool fullyInit;
        public static new BepInEx.Logging.ManualLogSource Logger { get; private set; }

        public static RainMeadow.OnlineGameMode.OnlineGameModeType BattleRoyaleGameMode = new("Battle Royale", true);
        public static ProcessManager.ProcessID BattleRoyaleMenu = new("BattleRoyaleMenu", true);

        //public static HideTimer hideTimer;

        public void OnEnable()
        {
            instance = this;
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            Logger = base.Logger;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (init) return;
            init = true;

            try
            {
                // menus
                On.Menu.MainMenu.ctor += MainMenu_ctor;
                On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;

                // setup
                RainMeadow.OnlineGameMode.RegisterType(BattleRoyaleGameMode, typeof(ExternalBattleRoyaleGameMode), "A Free for all battle, storms approach as you battle to the top for safety.");

                // timer
                On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
                new Hook(
                    typeof(Lobby).GetMethod(nameof(Lobby.RequestedLobby)),
                    new Action<Action<Lobby, RPCEvent, string>, Lobby, RPCEvent, string>(LobbyRequestedLobby)
                );

                fullyInit = true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                fullyInit = false;
                //throw;
            }
        }

        public void LobbyRequestedLobby(Action<Lobby, RPCEvent, string> orig, Lobby self, RPCEvent request, string key)
        {
            if (self.gameMode is ExternalBattleRoyaleGameMode)
            {
                if ((self.gameMode as ExternalBattleRoyaleGameMode).battleRoyaleData.battleRoyaleState != BattleRoyaleStatus.GameNotStarted)
                {
                    orig(self, request, key);
                }
                else
                {
                    request.from.QueueEvent(new GenericResult.Fail(request));
                }
                return;
            }
            orig(self, request, key);
            
        }

        private void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);
            // if(OnlineManager.lobby != null && OnlineManager.lobby.gameMode is ExternalBattleRoyaleGameMode tgm)
            // {
            //     hideTimer = new HideTimer(self, self.fContainers[0], tgm);
            //     self.AddPart(hideTimer);  // Add timer to HUD system
            // }
        }

        private void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == BattleRoyaleMenu)
            {
                self.currentMainLoop = new BattleRoyaleMenu(self);
            }

            orig(self, ID);
        }

        private void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);

            if (!fullyInit)
            {
                self.manager.ShowDialog(new Menu.DialogNotify("Battle Royale failed to start", self.manager, null));
                return;
            }
        }

        public static List<AbstractPhysicalObject> GetOnlinePlayerObjects()
        {
            if (OnlineManager.lobby != null)
            {
                var avatarsOnline = OnlineManager.lobby.playerAvatars.Select(x => x.Value.FindEntity(true)).OfType<OnlinePhysicalObject>();
                var avatarsApo = avatarsOnline.Select(x => x.apo);
                List<AbstractPhysicalObject> returnList = new();
                foreach (AbstractPhysicalObject avatar in avatarsApo)
                {
                    returnList.Add(avatar);
                }
                return returnList;
            }
            return null;
        }
    }
}