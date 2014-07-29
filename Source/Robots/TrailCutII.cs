// -------------------------------------------------------------------------------
//
//		TrailCutII-II (18 juillet 2014)
//		version 2.2014.7.13h30
//		Author : https://www.facebook.com/ab.hacid
//
// -------------------------------------------------------------------------------
#region cBot Comments
//
//	Robot 
//	Multi indicateurs avec seuil de declenchement (ceilSignal), 
//	gerant un trail stop, 
//	une cloture anticipee des pertes (Cut Loss) 
//	une martingale selective, 
//	Possibilite d'achat ou de vente seulement,
//	Signaux sur Tick ou sur nouvelle barre
//
//	Utiliser : (parametres a modifier avant de tester : Symbol, Timeframe, WprSource)
//			Symbol							=	GBPUSD
//			TimeFrame						=	D1
//			Volume							=	100000
//
//			OnTick							=	Non						//	Declanchement des ordres sur chaque nouveau tick ou sur chaque nouvelle barre
//          Stop Loss						=	150 pips
//          Take Profit						=	1000 pips				
//			Cut Loss						=	Non						//	Coupe les pertes a 50%, 66% du StopLoss initial
//			Buy Only						=	Non						//	Execute seulement des ordres sur signaux d'achat
//			Sell Only						=	Non						//	Execute seulement des ordres sur signaux de vente

//			Martingale						=	Oui						//	En cas de perte inverse la position avec un facteur de 1.5*Volume initial
//
//			Trail Start						=	3000					//	Debut du mouvement du stopLoss
//			Trail Step						=	3						//	Pas du Mouvement de trailling
//			Trail Stop						=	29						//	Minimum du StopLoss
//
//			WPR Source						=	Open					
//			WPR Period						=   17
//			WPR Overbuy Ceil				=	-20						//	Seuil d'oversell
//			WPR Oversell Ceil				=	-80						//	Seuil d'overbuy
//			WPR Magic Number				=	2						//	Permet d'etendre le temps de detection du tradeType et cree plus de signaux (Magic)
//			WPR Min/Max Period				=	114						//	Periode pendant laquelle on calcule le minimum et le maximum pour detecter l'etendue du range
//			WPR Exceed MinMax				=	2						//	Decalage par rapport au Minimum et au Maximum pour cloturer les positions
//
//			MBFXLen							=	13
//			MBFX Filter						=	5
//			ZzDeph							=	12
//			ZzDeviation						=	5
//			ZzBackStep						=	3
//
//			commission						=	37.6 per Million
//			Spread fixe						=	1pip
//			Starting Capital				=	50000
//
//	Results :
//          Resultats			=	entre le 01/04/2011 et 18/7/2014 a 11:30 gain de 34534 euros(+69%).
//			Net profit			=	34534.34 euros
//			Ending Equity		=	34534.34 euros
//			Ratio de Sharpe		=	1.75 sur achats 2.23 sur ventes
//			Ratio de Storino	=	-    sur achats - sur ventes
//
// -------------------------------------------------------------------------------
//			Utiliser en trading reel a vos propres risques. 
//			l'effet de levier comportant un risque de perte supérieure au capital investi.
// -------------------------------------------------------------------------------
#endregion

using cAlgo;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

using cAlgo.Lib;
using System;
using System.Text;

namespace cAlgo.Robots
{
	[Robot("TrailCutII",TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
	public class TrailCutII : Robot
	{
		#region cBot Parameters
		[Parameter("Volume", DefaultValue = 100000, MinValue = 0)]
		public int InitialVolume { get; set; }

		[Parameter("OnTick", DefaultValue = false)]
		public bool isOnTick { get; set; }

		[Parameter("Stop Loss", DefaultValue = 150)]
		public int StopLoss { get; set; }

		[Parameter("Take Profit", DefaultValue = 1000)]
		public int TakeProfit { get; set; }

		[Parameter("Cut Loss", DefaultValue = false)]
		public bool cutLoss { get; set; }

		[Parameter("Buy Only", DefaultValue = false)]
		public bool buyOnly { get; set; }

		[Parameter("Sell Only", DefaultValue = false)]
		public bool sellOnly { get; set; }

		[Parameter("Martingale", DefaultValue = false)]
		public bool Martingale { get; set; }

		[Parameter("Trail Start", DefaultValue = 3000, MinValue = 1)]
		public int TrailStart { get; set; }

		[Parameter("Trail Step", DefaultValue = 3, MinValue = 0)]
		public int TrailStep { get; set; }

		[Parameter("Trail Stop", DefaultValue = 29, MinValue = 0)]
		public int TrailStop { get; set; }

		[Parameter("William Percent Range", DefaultValue = true)]
		public bool isWPRActif { get; set; }

		[Parameter("WPR Source")]		// Placer a Open
		public DataSeries wprSource { get; set; }

		[Parameter("WPR Period", DefaultValue = 17, MinValue = 1)]
		public int wprPeriod { get; set; }

		[Parameter("WPR Overbuy Ceil", DefaultValue = -20, MinValue = -100, MaxValue = 0)]
		public int wprOverbuyCeil { get; set; }

		[Parameter("WPR Oversell Ceil", DefaultValue = -80, MinValue = -100, MaxValue = 0)]
		public int wprOversellCeil { get; set; }

		[Parameter("WPR Magic Number", DefaultValue = 2, MinValue = 0)]
		public int WprMagicNumber { get; set; }

		[Parameter("WPR Min/Max Period", DefaultValue = 114)]
		public int wprMinMaxPeriod { get; set; }

		[Parameter("WPR Exceed MinMax", DefaultValue = 2)]
		public int wprExceedMinMax { get; set; }

		[Parameter("Zigzag Kwan MBFX Timing", DefaultValue = false)]
		public bool isMBFXActif { get; set; }

		[Parameter("MBFX Len", DefaultValue = 4, MinValue = 0)]
		public int mbfxLen { get; set; }

		[Parameter("MBFX Filter", DefaultValue = -1.0)]
		public double mbfxFilter { get; set; }

		[Parameter("Zigzag", DefaultValue = false)]
		public bool isZZActif { get; set; }

		[Parameter(DefaultValue = 12)]
		public int ZzDepth { get; set; }

		[Parameter(DefaultValue = 5)]
		public int ZzDeviation { get; set; }

		[Parameter(DefaultValue = 3)]
		public int ZzBackStep { get; set; }

		[Parameter("Double Candle", DefaultValue = false)]
		public bool isDCActif { get; set; }

		[Parameter("Trend Magic", DefaultValue = false)]
		public bool isTMActif { get; set; }


		[Parameter("Trend Magic CCIPeriod", DefaultValue = 50)]
		public int TMCciPeriod { get; set; }

		[Parameter("Trend Magic ATRPeriod", DefaultValue = 5)]
		public int TMAtrPeriod { get; set; }

		#endregion

		#region cBot globals
		// Slippage maximun en point, si l'execution de l'ordre impose un slippage superieur, l'ordre n'est pas execute.
		private const double slippage = 2;
		// Prefixe des ordres passes par le robot
		private const string botPrefix = "TCI";
		// Label des  ordres passes par le robot
		private string botLabel;

		private ZigZag zigZag;
		private WilliamsPercentRange wpr;
		private ZigzagKwanMBFXTiming zigzagKwanMBFXTiming;
		private TrendMagic trendMagic;
		private CommodityChannelIndex _cci; 

		private double zigZagPrevValue; 

		#endregion

		#region cBot Events
		protected override void OnStart()
		{
			base.OnStart();
			botLabel = string.Format("{0}-{1} {2}", botPrefix, Symbol.Code, TimeFrame);

			Print(this.botName());


			wpr = Indicators.GetIndicator<WilliamsPercentRange>(wprSource, wprPeriod, wprOverbuyCeil, wprOversellCeil, WprMagicNumber, wprMinMaxPeriod, wprExceedMinMax);
			
			// ZigZag Kwan MBFX Timing ou Beta
			zigzagKwanMBFXTiming = Indicators.GetIndicator<ZigzagKwanMBFXTiming>(mbfxLen, mbfxFilter);

			zigZag = Indicators.GetIndicator<ZigZag>(ZzDepth, ZzDeviation, ZzBackStep);

			trendMagic = Indicators.GetIndicator<TrendMagic>(TMCciPeriod, TMAtrPeriod);

			_cci = Indicators.CommodityChannelIndex(TMCciPeriod);

			Positions.Opened += OnPositionOpened;
			Positions.Closed += OnPositionClosed;
		}

		protected override void OnStop()
		{
			base.OnStop();
			this.closeAllPositions();

		}

		protected void OnPositionOpened(PositionOpenedEventArgs args)
		{
		}

		protected void OnPositionClosed(PositionClosedEventArgs args)
		{
			Position position = args.Position;

			// Gere une Martingale selective.
			if (Martingale && (position.Pips < 0))
				splitAndExecuteOrder(position.isBuy() ? TradeType.Sell : TradeType.Buy, position.Volume * 1.5, botPrefix + "Mart-");
            
            Print(position.log(this,true));
		}

		// Controle des positions et relance.        
		protected override void OnTick()
		{
			closeLoss();

			trailStop();

			if (isOnTick)
				buyAndSell();
		}

		// A Chaque nouvelle bougie on peut evaluer la possibilite d'achat ou de vente ou de neutralite
		protected override void OnBar()
		{
			if (!isOnTick)
				buyAndSell();

		}

		protected override void OnError(Error error)
		{
			string errorText = "";
			switch (error.Code)
			{
				case ErrorCode.BadVolume: errorText = "Bad volume";
					break;
				case ErrorCode.TechnicalError: errorText = "Technical Error";
					break;
				case ErrorCode.NoMoney: errorText = "No Money";
					break;
				case ErrorCode.Disconnected: errorText = "Disconnected";
					break;
				case ErrorCode.MarketClosed: errorText = "Market Closed";
					break;
			}

			if (error.Code == ErrorCode.NoMoney || error.Code == ErrorCode.TechnicalError)
			{
				Notifications.SendEmail("Robot@whatever.com", "ab.hacid@gmail.com", "Robot Error", errorText + "error\n Action: Robot Stopped");
				OnStop();
			}

			if (error.Code == ErrorCode.BadVolume || error.Code == ErrorCode.Disconnected)
			{

				Print("Error:" + errorText);
			}

			if (error.Code == ErrorCode.MarketClosed)
			{
				//remove all pending orders etc
				//send email to with the balance, profit etc
				StringBuilder report = new StringBuilder("End of trading week report for the week of:");
				report.Append(DateTime.Now);
				report.Append("\n");
				report.Append("Account Balance:");
				report.Append(Account.Balance);
				report.Append("\n");

				Notifications.SendEmail("Robot@whatever.com", "ab.hacid@gmail.com", "End of week report", report.ToString());
				//better if this was done before close 
			}
		}


		#endregion

		#region cBot Predicate





		
		#endregion

		#region cBot Action

		// Gere les cloture des positions pertes selon l'adage laisser courrir les gains, cloturer les pertes.
		private void closeLoss()
		{
			foreach (var position in Positions.FindAll(this.botName(), Symbol))
			{
				if (cutLoss && position.TakeProfit.HasValue && position.StopLoss.HasValue)
				{
					string label = position.Comment.Substring(position.Comment.Length - 1, 1);
					double? percentLoss =position.percentLoss(this);

					if (percentLoss.HasValue && (((percentLoss <= -0.33) && (label == "1")) || ((percentLoss <= -0.66) && (label == "2"))))
						this.closePosition(position);

				}
			}
		}



		// Decoupe un ordre en trois ordres de volume 1/2, 3/10, 2/10 du volume initial demande.
		private void splitAndExecuteOrder(TradeType tradeType, double volume, string prefixLabel)
		{
			const double percent1 = 0.5;
			const double percent2 = 0.3;
			const double percent3 = 1 - percent1 - percent2;

			long volume1 = (long)Math.Floor(volume * percent1);
			long volume2 = (long)Math.Floor(volume * percent2);
			long volume3 = (long)Math.Floor(volume * percent3);

			executeOrder(tradeType, volume1, String.Format("{0}-{1}-1", prefixLabel, tradeType));
			executeOrder(tradeType, volume2, String.Format("{0}-{1}-2", prefixLabel, tradeType));
			executeOrder(tradeType, volume3, String.Format("{0}-{1}-3", prefixLabel, tradeType));

		}

		private void executeOrder(TradeType tradeType, long volume, string label)
		{
			if (volume <= 0)
				return;

			// il faut que le volume soit un multiple de "microVolume".
			long v = Symbol.NormalizeVolume(volume, RoundingMode.ToNearest);

			if (v > 0)
			{
				var result = ExecuteMarketOrder(tradeType, Symbol, v, this.botName(), StopLoss, TakeProfit, slippage, label);
				if (!result.IsSuccessful)
					Print("error : {0}, {1}", result.Error, v);
			}
		}

		// Gere la prise de position
		private void buyAndSell()
		{
			TradeType? tradeType = signalStrategie();

			if (tradeType.HasValue)
				splitAndExecuteOrder(tradeType.Value, InitialVolume, botLabel);
		}

		// Gere le stop suiveur dynamique
		private void trailStop()
		{
			foreach (Position position in Positions)
			{
				if (position.Pips > TrailStart)
				{
					double actualPrice = position.isBuy() ? Symbol.Bid : Symbol.Ask;
					int factor = position.isBuy() ? 1 : -1;

					double? newStopLoss = position.StopLoss;

					// Stop a ZERO
					if ((position.EntryPrice - newStopLoss) * factor > 0)
						newStopLoss = position.EntryPrice;

					if ((actualPrice - newStopLoss) * factor > TrailStep * Symbol.PipSize)
					{
						newStopLoss += factor * TrailStep * Symbol.PipSize;

						if (newStopLoss != position.StopLoss)
							ModifyPosition(position, newStopLoss, position.TakeProfit.Value);
					}
				}

			}

		}
		#endregion

		#region cBot Strategie
		// STRATEGIE DE TRADING
		// Renvoie un tradeType d'achat, de vente ou neutre. C'est ici qu'il faut ecrire la strategie de declanchement des ordres
		// retourne : Nothing : pas de tradeType (neutre), Buy : achat, Sell: vente

		private TradeType? signalStrategie()
		{
			int ceilSignal = isZZActif.toInt()+isWPRActif.toInt()+isDCActif.toInt()+isZZActif.toInt()+isTMActif.toInt();

			if (ceilSignal >0)
			{
				int signal = 0;

				if (isZZActif)
					signal += zigzagKwanMBFXTimingStrategie().toInt();

				if(isWPRActif)
					signal += williamsPercentRangeStrategie().toInt();

				if(isDCActif)
					signal += doubleCandleStrategie().toInt();

				if(isZZActif)
					signal += zigzagStrategie().toInt();

				if(isTMActif)
					signal += trendMagicStrategie().toInt();


				if (!sellOnly && signal >= ceilSignal)
					return TradeType.Buy;
				else
					if (!buyOnly && signal <= -ceilSignal)
						return TradeType.Sell;
			}
			

			return null;
		}



		private TradeType? zigzagStrategie()
		{
			double lastValue = zigZag.Result.LastValue;

			if (!double.IsNaN(lastValue))
			{
				zigZagPrevValue = lastValue;

				if (!(this.existBuyPositions()) && (lastValue < zigZagPrevValue))
					return TradeType.Buy;
				else if (!(this.existSellPositions()) && (lastValue > zigZagPrevValue && zigZagPrevValue > 0.0))
					return TradeType.Sell;
			}

			return null;
		}

		private TradeType? zigzagKwanMBFXTimingStrategie()
		{
			if (!(this.existBuyPositions()) && (zigzagKwanMBFXTiming.tradeActionIndicatorDataSeries.HasCrossedAbove(0.5,0)))
			{
				this.closeAllSellPositions();
				return TradeType.Buy;
			}
			else if (!(this.existSellPositions()) && (zigzagKwanMBFXTiming.tradeActionIndicatorDataSeries.HasCrossedBelow(-0.5,0)))
			{
				this.closeAllBuyPositions();
				return TradeType.Sell;
			}

			return null;
		}

		// Strategie : deux bougies haussieres donne un tradeType d'achat, deux bougies baissieres un tradeType de vente
		private TradeType? doubleCandleStrategie()
		{

			double step = 7 * Symbol.PipSize;
			int LastBarIndex = MarketSeries.Close.Count - 2;
			int PrevBarIndex = LastBarIndex - 1;

			if ((MarketSeries.Close[LastBarIndex] > MarketSeries.Open[LastBarIndex] + step) && (MarketSeries.Close[PrevBarIndex] > MarketSeries.Open[PrevBarIndex] + step))
				return TradeType.Buy;

			if ((MarketSeries.Close[LastBarIndex] + step < MarketSeries.Open[LastBarIndex]) && (MarketSeries.Close[PrevBarIndex] + step < MarketSeries.Open[PrevBarIndex]))
				return TradeType.Sell;

			return null;
		}
		
		// Strategie selon indicateur Williams Percent Range
		private TradeType? williamsPercentRangeStrategie()
		{

			if (wpr.IsExceedLow)
				this.closeAllSellPositions();
			else
				if (wpr.IsExceedHigh)
					this.closeAllBuyPositions();

			if ((!(this.existBuyPositions()) || !isOnTick) && wpr.IsCrossAboveOversell)
			{
				this.closeAllSellPositions();

				return TradeType.Buy;
			}
			else if (!(this.existSellPositions()) && wpr.IsCrossBelowOverbuy && wpr.IsExceedHigh)
			{
				this.closeAllBuyPositions();

				return TradeType.Sell;
			}

			return null;

		}

		private TradeType? trendMagicStrategie()
		{
			if (!(this.existBuyPositions()) && trendMagic.BufferUpOutput.HasCrossedAbove(Symbol.Ask,1) && _cci.Result.LastValue>0)
			{
				this.closeAllSellPositions();
				return TradeType.Buy;
			}
			else
				if (!(this.existSellPositions()) && trendMagic.BufferDnOutput.HasCrossedBelow(Symbol.Bid,1) && _cci.Result.LastValue<0)
				{
					this.closeAllBuyPositions();
					return TradeType.Sell;
				}

			return null;
		}

		#endregion
	}
}


