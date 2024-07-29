USE [ToDoListDb]
GO

/****** Object:  Table [dbo].[Signals]    Script Date: 7/29/2024 4:42:56 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Signals](
	[Id] [uniqueidentifier] NOT NULL,
	[Symbol] [int] NOT NULL,
	[Timeframe] [int] NOT NULL,
	[SignalType] [int] NOT NULL,
	[StartDate] [datetime] NOT NULL,
	[EnterPrice] [float] NOT NULL,
	[StopLostPrice] [float] NOT NULL,
	[TakeProfitPrice] [float] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[WinChanceEstimate] [tinyint] NOT NULL,
	[SignalProvider] [int] NOT NULL,
 CONSTRAINT [PK_Signals] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


