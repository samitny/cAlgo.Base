///////////////////////////////////////////////////////////////////////////////////////////////////////////////
//	Zelphin Scalper II
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
// programme original de Mark Jensen Zelphin Scalper http://ctdn.com/algos/cbots/show/429
// modifie par Abdallah Hacid, https://www.facebook.com/ab.hacid 
// modified date : 17 juillet 2014

// Uses simple moving average and stochastic oscillator to find a good trade opportunity
// tested using GBPUSD symbol and 5m chart.


//	Symbole			=	GBPUSD
//	Timeframe		=	m5
//
//	Source			=	Open
//	Period			=	35
//	Volume			=	100k
//
//	TakeProfit		=	300
//	StopLoss		=	53
//	TrailStart		=	29
//	Trail			=	3
//
//	MaType			=	Exponential
//	K Period		=	5
//	D Period		=	3
//	K Slowing		=	3
//
//	Results :
//          Resultats			=	entre le 01/04/2011 et 17/7/2014 a 19:00 gain de 5303 euros(+11%).
//			Net profit			=	5303,01 euros
//			Ending Equity		=	5303,01 euros
//			Ratio de Sharpe		=	0.13
//			Ratio de Storino	=	0.17
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
//												Use at own risk
///////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class ZephynScalperII : Robot
	{
		#region Prameters
        [Parameter("Source SMA")]
        public DataSeries Source_SMA { get; set; }

		[Parameter("Periods SMA", DefaultValue = 37)]
        public int Periods_SMA { get; set; }

        [Parameter("Volume", DefaultValue = 100000, MinValue = 10000)]
        public int Volume { get; set; }

        [Parameter("TakeProfit", DefaultValue = 300, MinValue = 1)]
        public int TakeProfit { get; set; }

        [Parameter("Stop Loss", DefaultValue = 53, MinValue = 1)]
        public int StopLoss { get; set; }

        [Parameter("Trail start", DefaultValue = 29, MinValue = 1)]
        public int Trail_start { get; set; }

        [Parameter("Trail", DefaultValue = 3, MinValue = 1)]
        public int Trail { get; set; }

        [Parameter(DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType MaType { get; set; }

        [Parameter("K Periods", DefaultValue = 5)]
        public int KPeriods { get; set; }

        [Parameter("D Periods", DefaultValue = 3)]
        public int DPeriods { get; set; }

        [Parameter("K Slowing", DefaultValue = 3)]
        public int K_Slowing { get; set; }
		#endregion

		#region Globals

        private SimpleMovingAverage _SMA;
        private StochasticOscillator _SOC;
		// Nom du robot
		private const string botName = "ZephynScalperII";
		private const int highCeil = 80;
		private const int lowCeil = 20;
		#endregion

		#region cBot Events
		protected override void OnStart()
        {
			base.OnStart();
            _SMA = Indicators.SimpleMovingAverage(Source_SMA, Periods_SMA);
            _SOC = Indicators.StochasticOscillator(KPeriods, K_Slowing, DPeriods, MaType);

            Positions.Closed += OnPositionClosed;


        }

		protected override void OnStop()
		{
			base.OnStop();
			closePositions();

		}

        protected override void OnTick()
        {
			foreach (var position in Positions.FindAll(botName, Symbol))
            {
                if (position.Pips > Trail_start)
                {
					double actualPrice = isBuy(position) ? Symbol.Bid : Symbol.Ask;
					int factor = isBuy(position) ? 1 : -1;
				
					double? newStopLoss = position.StopLoss;

					// Stop a ZERO
					if ((position.EntryPrice - newStopLoss) * factor > 0)
						newStopLoss = position.EntryPrice;

					if ((actualPrice - newStopLoss)*factor > Trail * Symbol.PipSize)
                    {
						newStopLoss += factor * Trail * Symbol.PipSize;

						if (newStopLoss!=position.StopLoss)
							ModifyPosition(position, newStopLoss, position.TakeProfit.Value);
                    }
                }
            }


            if (!isBuyPositions && isSOCBuySignal)
			{
				closePositions(TradeType.Sell);
                Open(TradeType.Buy);
			}
            else
				if (!isSellPositions && isSOCSellSignal)
				{
					closePositions(TradeType.Buy);
					Open(TradeType.Sell);
				}
                       

        }

		private void OnPositionClosed(PositionClosedEventArgs args)
        {
        }
		#endregion

		#region cBot Action
		private void Open(TradeType tradeType)
        {
				ExecuteMarketOrder(tradeType, Symbol, Volume, botName, StopLoss, TakeProfit);
        }

		private void closePosition(Position position)
		{
			var result = ClosePosition(position);

			if (!result.IsSuccessful)
				Print("error : {0}", result.Error);

		}

		// Cloture toutes les positions de type "tradeType"
		private void closePositions(TradeType tradeType)
		{
			foreach (Position position in Positions.FindAll(botName, Symbol, tradeType))
				closePosition(position);

		}

		// Cloture toutes les positions ouvertes
		private void closePositions()
		{
			closePositions(TradeType.Buy);
			closePositions(TradeType.Sell);
		}
		#endregion

		#region cBot Predicate

		private bool isSOCBuySignal
		{
			get
			{
				return (_SMA.Result.LastValue < Symbol.Bid) &&
						(_SOC.PercentD.LastValue < lowCeil) &&
						(_SOC.PercentK.LastValue < lowCeil) &&
						(_SOC.PercentK.LastValue > _SOC.PercentD.LastValue);
			}

		}

		private bool isSOCSellSignal
		{
			get 
			{ 
				return	(_SMA.Result.LastValue > Symbol.Ask) && 
						(_SOC.PercentD.LastValue > highCeil) &&
						(_SOC.PercentK.LastValue > highCeil) &&
						(_SOC.PercentK.LastValue < _SOC.PercentD.LastValue);
			}

		}
		private bool isBuy(Position position)
		{
			return TradeType.Buy == position.TradeType;
		}
		private bool isBuy(TradeType tradeType)
		{
			return TradeType.Buy == tradeType;
		}

		private bool isSell(Position position)
		{
			return TradeType.Sell == position.TradeType;
		}

		private bool isSell(TradeType tradeType)
		{
			return TradeType.Sell == tradeType;
		}

		private bool isBuyPositions
		{
			get { return Positions.Find(botName, Symbol, TradeType.Buy) != null; }

		}

		private bool isSellPositions
		{
			get { return Positions.Find(botName, Symbol, TradeType.Sell) != null; }
		}

		private bool isBuyAndSellPositions
		{
			get { return isBuyPositions && isSellPositions; }
		}
		private bool isPosition
		{
			//get { return Positions.Find(botName,Symbol)!=null; }
			get { return isBuyPositions || isSellPositions; }

		}

		private bool isNoPosition
		{
			get { return !isPosition; }
		}


		#endregion

		#region cBot Utils
		private int OpenedTrades
		{
			get { return Positions.FindAll(botName, Symbol).Count(); }
		}
		#endregion

	}
}
