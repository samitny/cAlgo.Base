// -------------------------------------------------------------------------------
//
//    This is a Template used as a guideline to build your own Robot. 
//    Please use the  Feedback  tab to provide us with your suggestions about cAlgo s API.
//
// -------------------------------------------------------------------------------

using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using cAlgo.API.Requests;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]
    public class Bertho : Robot
    {
        [Parameter("Volume initial", DefaultValue = 10000, MinValue = 0)]
        public int InitialVolume { get; set; }

        [Parameter("Stop Loss", DefaultValue = 20)]
        public int StopLoss { get; set; }

        [Parameter("Take Profit", DefaultValue = 30)]
        public int TakeProfit { get; set; }

        private Random random = new Random();
        private Position position;

        public int suiteGains = 0;
        public int cumulPertes = 0;
        public int _volume = 0;
        public TradeType sensPosition;

        protected override void OnStart()
        {
            ExecuteOrder(InitialVolume, GetRandomTradeCommand());
        }

        private void ExecuteOrder(int volume, TradeType tradeType)
        {
            var request = new MarketOrderRequest(tradeType, volume)
            {
                Label = "BerthoRobot",
                StopLossPips = StopLoss,
                TakeProfitPips = TakeProfit
            };
            Trade.Send(request);

        }

        protected override void OnTick()
        {
            // Put your core logic here
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }

        protected override void OnPositionOpened(Position openedPosition)
        {
            position = openedPosition;
        }

        protected override void OnPositionClosed(Position closedPosition)
        {
            if (closedPosition.GrossProfit > 0)
            {
                suiteGains += 1;
                if (suiteGains == 1)
                {
                    if (position.Volume != cumulPertes)
                    {
                        _volume = (int)cumulPertes;/////////////////////
                        sensPosition = position.TradeType;
                        cumulPertes = 0;
                    }
                    else
                    {
                        _volume = (int)position.Volume;/////////////////////////
                        sensPosition = position.TradeType;
                    }
                }
                else if (suiteGains == 2)
                {
                    _volume = (int)InitialVolume;////////////////////////
                    sensPosition = position.TradeType;
                    cumulPertes = 0;
                }
            }
            else
            {
                cumulPertes += (int)position.Volume;
                suiteGains = 0;
                _volume = (int)position.Volume;////////////////////////
                if (position.TradeType == TradeType.Buy)
                {
                    sensPosition = TradeType.Sell;
                }
                else
                {
                    sensPosition = TradeType.Buy;
                }
            }

            ExecuteOrder((int)_volume, (TradeType)sensPosition);
        }

        protected override void OnError(Error error)
        {
            if (error.Code == ErrorCode.BadVolume)
            {
                Print("Erreur de Volume");
            }
        }

        private TradeType GetRandomTradeCommand()
        {
            return random.Next(2) == 0 ? TradeType.Buy : TradeType.Sell;
        }

    }
}