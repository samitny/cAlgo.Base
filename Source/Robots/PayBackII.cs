// -------------------------------------------------------------------------------
//
//    PayBack modifie (28 juin 2014)
//	  version 1.2014.7.1.19h
//    Abdallah HACID (c) 2014
//    http://www.babooclic.com
//
//	Utiliser : 
//			Volume				=	100000
//          SL					=	57 pips
//          TP					=	150 pips
//			commission			=	37.6 per Million
//			Spread fixe			=	1pip
//			Starting Capital	=	50000
//
//	Results :
//          sur GBPUSD en h1 entre le 1/1/2014 et 1/7/2014 a 19h30 gain de 9482 euros(+19%).
//			Net profit			=	9481.93
//			Ending Equity		=	10164.18 euros
//			Ratio de Sharpe		=	0.24
//			Ratio de Storino	=	0.55
// -------------------------------------------------------------------------------

using System;
using cAlgo.API;
using cAlgo;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]
    public class PAYBACKII : Robot
    {
        [Parameter("Initial Volume", DefaultValue = 100000, MinValue = 0)]
        public int InitialVolume { get; set; }
        [Parameter("Stop Loss", DefaultValue = 57)]
        public int StopLoss { get; set; }
        [Parameter("Take Profit", DefaultValue = 150)]
        public int TakeProfit { get; set; }

        const long microVolume = 1000;
        const string botLabel = "PB-";


        protected override void OnStart()
        {
            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;

            relanceOrders();
        }

        private void relanceOrders()
        {
            manageOpen(TradeType.Buy, InitialVolume);
            manageOpen(TradeType.Sell, InitialVolume);
        }

        private void manageOpen(TradeType tradeType, long volume, string prefixLabel = botLabel)
        {
            int nVolumePartition = 10, part1 = 5, part2 = 3;
            long nVol = (long)Math.Floor((double)(volume / (microVolume * nVolumePartition)));
            long partialVolume = nVol * microVolume;

            var result1 = ExecuteMarketOrder(tradeType, Symbol, partialVolume * part1, prefixLabel + tradeType.ToString() + "-1");
            var result2 = ExecuteMarketOrder(tradeType, Symbol, partialVolume * part2, prefixLabel + tradeType.ToString() + "-2");
            var result3 = ExecuteMarketOrder(tradeType, Symbol, volume - (part1 + part2) * partialVolume, prefixLabel + tradeType.ToString() + "-3");
        }

        private void manageClose()
        {
            foreach (var position in Positions)
            {
                if (position.TakeProfit.HasValue)
                {
                    string labelType = position.Label.Substring(position.Label.Length - 1, 1);
                    double potentialGainPips = ((position.TradeType == TradeType.Buy) ? 1 : -1) * (position.TakeProfit.Value - position.EntryPrice) / Symbol.PipSize;
                    double potentialLosePips = ((position.TradeType == TradeType.Buy) ? 1 : -1) * (position.StopLoss.Value - position.EntryPrice) / Symbol.PipSize;
                    double percentGain = position.Pips / potentialGainPips;
                    double percentLose = -position.Pips / potentialLosePips;

                    if ((percentGain >= 0.43) && (labelType == "3"))
                        ClosePosition(position);

                    if ((percentGain >= 0.76) && (labelType == "2"))
                        ClosePosition(position);

                    if ((percentLose <= -0.33) && (labelType == "1"))
                        ClosePosition(position);

                    if ((percentLose <= -0.66) && (labelType == "2"))
                        ClosePosition(position);

                }
            }
        }


        protected void OnPositionOpened(PositionOpenedEventArgs args)
        {
            var position = args.Position;

            Limits limits = new Limits(position.TradeType, position.EntryPrice, Symbol.PipSize, TakeProfit, StopLoss);

            //position.LimitsExt().SL_PIPS = StopLoss;
            //position.LimitsExt().TP_PIPS = TakeProfit;

            // double stopLoss = position.TradeType == TradeType.Buy ? position.EntryPrice - Symbol.PipSize * StopLoss : position.EntryPrice + Symbol.PipSize * StopLoss;
            // double takeProfit = position.TradeType == TradeType.Buy ? position.EntryPrice + Symbol.PipSize * TakeProfit : position.EntryPrice - Symbol.PipSize * TakeProfit;

            ModifyPosition(position, limits.StopLoss, limits.TakeProfit);
        }

        protected void OnPositionClosed(PositionClosedEventArgs args)
        {

            if (args.Position.Pips < 0)
                manageOpen(Tools.inverseTradeType(args.Position), args.Position.Volume, botLabel + "Mart-");

            if (Positions.Count == 0)
                relanceOrders();
        }


        protected override void OnTick()
        {
            manageClose();

        }
        protected override void OnError(Error error)
        {
            if (error.Code != ErrorCode.BadVolume)
            {
                Print("erreur : " + error.Code);
                Stop();
            }
        }



    }
}


