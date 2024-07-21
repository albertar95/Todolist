USE [ToDoListDb]
GO

/****** Object:  Table [dbo].[AugmentedCandles]    Script Date: 7/21/2024 5:09:37 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[AugmentedCandles](
	[Id] [uniqueidentifier] NOT NULL,
	[Symbol] [int] NOT NULL,
	[Timeframe] [int] NOT NULL,
	[Time] [datetime] NOT NULL,
	[Open] [float] NOT NULL,
	[High] [float] NOT NULL,
	[Low] [float] NOT NULL,
	[Close] [float] NOT NULL,
	[Volume] [int] NOT NULL,
	[Sma50] [float] NOT NULL,
	[Sma100] [float] NOT NULL,
	[Ema12] [float] NOT NULL,
	[Ema26] [float] NOT NULL,
	[MACDLine] [float] NOT NULL,
	[SignalLine] [float] NOT NULL,
	[Histogram] [float] NOT NULL,
	[RSI] [float] NOT NULL,
 CONSTRAINT [PK_AugmentedCandles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


