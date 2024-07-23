USE [ToDoListDb]
GO

/****** Object:  Index [IX_SymbolnTimeframe]    Script Date: 7/23/2024 11:05:31 AM ******/
CREATE NONCLUSTERED INDEX [IX_SymbolnTimeframe] ON [dbo].[AugmentedCandles]
(
	[Symbol] ASC,
	[Timeframe] ASC
)
INCLUDE([Id],[Time],[Open],[High],[Low],[Close],[Volume],[Sma50],[Sma100],[Ema12],[Ema26],[MACDLine],[SignalLine],[Histogram],[RSI]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO


