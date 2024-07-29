USE [ToDoListDb]
GO

/****** Object:  Table [dbo].[SignalResults]    Script Date: 7/29/2024 4:42:59 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SignalResults](
	[Id] [uniqueidentifier] NOT NULL,
	[SignalId] [uniqueidentifier] NOT NULL,
	[Status] [int] NOT NULL,
	[ClosePrice] [float] NOT NULL,
	[ProfitPercentage] [float] NOT NULL,
	[Duration] [int] NOT NULL,
	[CloseDate] [datetime] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[ClosureType] [int] NOT NULL,
 CONSTRAINT [PK_SignalResults] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[SignalResults]  WITH CHECK ADD  CONSTRAINT [FK_SignalResults_Signals] FOREIGN KEY([SignalId])
REFERENCES [dbo].[Signals] ([Id])
GO

ALTER TABLE [dbo].[SignalResults] CHECK CONSTRAINT [FK_SignalResults_Signals]
GO


