USE [TodolistDb]
GO

/****** Object:  Table [dbo].[NotifyLogs]    Script Date: 8/9/2024 4:34:18 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[NotifyLogs](
	[Id] [uniqueidentifier] NOT NULL,
	[Symbol] [int] NOT NULL,
	[Timeframe] [int] NOT NULL,
	[LastNotify] [datetime] NOT NULL,
 CONSTRAINT [PK_NotifyLogs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO


