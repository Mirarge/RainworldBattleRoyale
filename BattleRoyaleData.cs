using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MeadowBattleRoyale
{
    public enum BattleRoyaleStatus
        {
            GameNotStarted, //Still in lobby possibly
            PeaceTime, //The time before the storms start
            Regular, //Storms have started. The game has really begun.
            OneRoomRemainingWait, //The rooms around the final room are filling with rain now
            FinalBattle //Only one room doesn't have deathrain.

        }

    public class BattleRoyaleData : OnlineResource.ResourceData
    {
        private static int oneMinute = 60 * 20; //2o ticks per 60 seconds
        public List<OnlinePlayer> playersLeft = new();
        public int timeBeforeStormsStart = oneMinute; //One minute peace time
        public int stormTimer = 0;
        public int stormTimerMax = oneMinute * 2; //Two minutes before the rain in a room is deadly
        public int finalBattleTimer = oneMinute * 5; //5 minutes to fight the final battle

        public int _battleRoyaleState = 0;
        public BattleRoyaleStatus battleRoyaleState
        {
            get => (BattleRoyaleStatus)_battleRoyaleState;
            set => _battleRoyaleState = (int)value;
        }

        public SlugcatStats.Name slugcat = SlugcatStats.Name.White;
        public SlugcatStats.Timeline worldstate = SlugcatStats.Timeline.White;
        public string region;

        //public Dictionary<int, string> playerStartingRooms = new();
        public List<int> playerStartingRoomsKey = new();
        public List<string> playerStartingRoomsValue = new();

        public List<string> roomsWithoutRain = new();
        public List<string> roomsWithIncomingRain = new();
        public List<string> roomsWithRain = new();


        public BattleRoyaleData() { }

        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new RoyaleState(this);
        }



        private class RoyaleState : ResourceDataState
        {
            [OnlineField(nullable = true)]
            RainMeadow.Generics.DynamicUnorderedUshorts playersLeft;


            [OnlineField]
            public int timeBeforeStormsStart = 1200; //what the stormtimer is negated by when the game starts to give some time.
            [OnlineField]
            int stormTimer = 0; //The timer that ticks up.
            [OnlineField]
            int stormTimerMax = 1200; //What the timer needs to reached to be set to 0 again
            [OnlineField]
            int finalBattleTimer = 1200;


            [OnlineField]
            public int _battleRoyaleState = 0;

            public BattleRoyaleStatus battleRoyaleState
            {
                get => (BattleRoyaleStatus)_battleRoyaleState;
                set => _battleRoyaleState = (int)value;
            }
            [OnlineField]
            public SlugcatStats.Name slugcat = SlugcatStats.Name.White;

            [OnlineField]
            public SlugcatStats.Timeline worldstate = SlugcatStats.Timeline.White;

            [OnlineField]
            public string region;

            [OnlineField]
            public List<int> playerStartingRoomsKey = new();
            [OnlineField]
            public List<string> playerStartingRoomsValue = new();

            [OnlineField]
            public List<string> roomsWithoutRain = new();
            [OnlineField]
            public List<string> roomsWithIncomingRain = new();
            [OnlineField]
            public List<string> roomsWithRain = new();

            public RoyaleState() { }
            public RoyaleState(BattleRoyaleData battleRoyaleData)
            {
                playersLeft = new(battleRoyaleData.playersLeft.Select(p => p.inLobbyId).ToList());
                timeBeforeStormsStart = battleRoyaleData.timeBeforeStormsStart;
                battleRoyaleState = battleRoyaleData.battleRoyaleState;
                slugcat = battleRoyaleData.slugcat;
                worldstate = battleRoyaleData.worldstate;
                region = battleRoyaleData.region;
                playerStartingRoomsKey = battleRoyaleData.playerStartingRoomsKey;
                playerStartingRoomsValue = battleRoyaleData.playerStartingRoomsValue;
                roomsWithoutRain = battleRoyaleData.roomsWithoutRain;
                roomsWithIncomingRain = battleRoyaleData.roomsWithIncomingRain;
                roomsWithRain = battleRoyaleData.roomsWithRain;
                stormTimer = battleRoyaleData.stormTimer;
                stormTimerMax = battleRoyaleData.stormTimerMax;
                finalBattleTimer = battleRoyaleData.finalBattleTimer;
            }

            public override Type GetDataType() => typeof(BattleRoyaleData);

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                BattleRoyaleData battleRoyaleData = (BattleRoyaleData)data;
                battleRoyaleData.playersLeft = playersLeft.list.Select(i => OnlineManager.lobby.PlayerFromId(i)).Where(p => p != null).ToList();
                battleRoyaleData.timeBeforeStormsStart = timeBeforeStormsStart;
                battleRoyaleData.battleRoyaleState = battleRoyaleState;
                battleRoyaleData.slugcat = slugcat;
                battleRoyaleData.worldstate = worldstate;
                battleRoyaleData.region = region;
                battleRoyaleData.playerStartingRoomsKey = playerStartingRoomsKey;
                battleRoyaleData.playerStartingRoomsValue = playerStartingRoomsValue;
                battleRoyaleData.roomsWithoutRain = roomsWithoutRain;
                battleRoyaleData.roomsWithIncomingRain = roomsWithIncomingRain;
                battleRoyaleData.roomsWithRain = roomsWithRain;
                battleRoyaleData.stormTimer = stormTimer;
                battleRoyaleData.stormTimerMax = stormTimerMax;
                battleRoyaleData.finalBattleTimer = finalBattleTimer;
            }
        }
    }
}