﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Todolist.Models
{
    public class TradeModels
    {
        public enum Timeframe
        {
            M1 = 1, M5 = 5,M10 = 10,M15 = 15,H1 = 60,H4 = 240,D1 = 1440,W1 = 10080
        }
        public enum Symbol
        {
            SOLUSDT = 1, BTCUSDT = 2,EURUSD = 3
        }
        public class MarketDataCandle
        {
            public float close { get; set; }
            public string currency { get; set; }
            public string date_time { get; set; }
            public string endpoint { get; set; }
            public float high { get; set; }
            public float low { get; set; }
            public float open { get; set; }
            public string request_time { get; set; }
            public DateTime UrlRequestTime { get; set; }
            public string message { get; set; }
        }
        public class Candle
        {
            public Guid Id { get; set; }
            public Symbol Symbol { get; set; }
            public Timeframe Timeframe { get; set; }
            public DateTime Time { get; set; }
            public float Open { get; set; }
            public float High { get; set; }
            public float Low { get; set; }
            public float Close { get; set; }
            public int Volume { get; set; }
        }
        public class Signals
        {
            public Guid Id { get; set; }
            public Symbol Symbol { get; set; }
            public Timeframe Timeframe { get; set; }
            public SignalTypes SignalType { get; set; }
            public DateTime StartDate { get; set; }
            public float EnterPrice { get; set; }
            public float StopLostPrice { get; set; }
            public float TakeProfitPrice { get; set; }
            public DateTime CreateDate { get; set; }
            public bool IsActive { get; set; }
            public byte WinChanceEstimate { get; set; }
            public SignalProviders SignalProvider { get; set; }
            public virtual ICollection<SignalResults> SignalResults { get; set; }
        }
        public class SignalResults
        {
            public Guid Id { get; set; }
            public Guid SignalId { get; set; }
            public SignalResultStatus Status { get; set; }
            public float ClosePrice { get; set; }
            public float ProfitPercentage { get; set; }
            public int Duration { get; set; }
            public DateTime CloseDate { get; set; }
            public DateTime CreateDate { get; set; }
            public SignalResultClosureTypes ClosureType { get; set; }
            public virtual Signals Signal { get; set; }
        }
        public class SignalEstimate
        {
            public Guid Id { get; set; }
            public Guid AugId { get; set; }
            public LinesOnMacdPositions LinesOnMacdMapPosition { get; set; }
            public LinesPositions LinesPosition { get; set; }
            public SMAPositions SmaPosition { get; set; }
            public CandlesToSMAPositions CandlesToSmaPosition { get; set; }
            public HistogramPositions HistogramPosition { get; set; }
            public SignalTypes signalType { get; set; }
        }
        public class SignalProgress
        {
            public string Duration { get; set; }
            public string PriceProgress { get; set; }
            public LinesOnMacdPositions MacdMapPosition { get; set; }
            public LinesPositions LinePosition { get; set; }
            public SMAPositions smaPosition { get; set; }
            public double Profit { get; set; }
        }
        public enum SignalTypes
        {
            Bullish = 1, Bearish = 2, Sideway = 3, NotSet = 4
        }
        public enum SignalProviders
        {
            MaStrategy = 1, MaStrategyRevision = 2, RviStrategy = 3
        }
        public enum SignalResultStatus
        {
            successful = 1, equal = 2, unsuccessful = 3
        }
        public enum SignalResultClosureTypes
        {
            tpHitted = 1, slHitted = 2, closedInMiddleByProvider = 3
        }
        public enum SignalCreationStatus
        {
            Observing = 1, BullSignalCreated = 2, BearSignalCreated = 3
        }
        public enum LinesOnMacdPositions
        {
            BothUpperBaseLine = 1, BothNearBaseLine = 2, BothBellowBaseLine = 3, MacdUpperBaseLineAndSignalBellow = 4, SignalUpperBaseLineAndMacdBellow = 5, MacdNearBaseLineAndSignalBellow = 6, MacdNearBaseLineAndSignalUpper = 7, SignalNearBaseLineAndMacdBellow = 8, SignalNearBaseLineAndMacdUpper = 9
        }
        public enum LinesPositions
        {
            UpperProvider = 1, Equal = 2, UpperSignal = 3
        }
        public enum SMAPositions
        {
            Upper50 = 1, Equal = 2, Upper100 = 3
        }
        public enum CandlesToSMAPositions
        {
            CandlesUpperBoth = 1, CandlesUpper50AndBellow100 = 2, CandlesUpper100AndBellow50 = 3, CandlesBellowBoth = 4, CandlesNear50AndUpper100 = 5, CandlesNear50AndBellow100 = 6, CandlesNear100AndUpper50 = 7, CandlesNear100AndBellow50 = 8, CandlesNearBoth = 9
        }
        public enum HistogramPositions
        {
            UpperBaseLineAscending = 1, UpperBaseLineDescending = 2, BellowBaseLineAscending = 3, BellowBaseLineDescending = 4, unknown = 5
        }
    }
}