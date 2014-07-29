using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Thx : Robot
    {
        private MovingAverage _slowMa;

        [Parameter(DefaultValue = "ThxBot")]
        public string cBotLabel { get; set; }

        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("MA Type")]
        public MovingAverageType MAType { get; set; }

        [Parameter("Slow Periods", DefaultValue = 1300)]
        public int SlowPeriods { get; set; }

        [Parameter(DefaultValue = 10000)]
        public int Volume { get; set; }

        [Parameter("Stop Loss (pips)", DefaultValue = 100)]
        public int StopLoss { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 100)]
        public int TakeProfit { get; set; }

        [Parameter(DefaultValue = 3)]
        public int MaxPositions { get; set; }


        protected override void OnStart()
        {
            _slowMa = Indicators.MovingAverage(SourceSeries, SlowPeriods, MAType);
        }

        protected override void OnBar()
        {
            var cBotPositions = Positions.FindAll(cBotLabel);

            if (cBotPositions.Length > MaxPositions)
                return;

            var currentSlowMa = _slowMa.Result.Last(0);

            // Condition to Buy
            if (currentSlowMa > Symbol.Bid)
            {
                ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
            }
            if (currentSlowMa < Symbol.Bid)
            {
                ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
            }

        }


    }
}
