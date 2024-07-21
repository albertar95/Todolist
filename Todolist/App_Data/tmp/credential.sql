USE [ToDoListDb]
GO

/****** Object:  Table [dbo].[MarketDataCredentials]    Script Date: 7/21/2024 5:09:07 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[MarketDataCredentials](
	[Id] [uniqueidentifier] NOT NULL,
	[Symbol] [int] NOT NULL,
	[Timeframe] [int] NOT NULL,
	[ApiKey] [nvarchar](1000) NOT NULL,
	[Username] [nvarchar](100) NOT NULL,
	[Password] [nvarchar](max) NOT NULL,
	[CallCounter] [int] NOT NULL,
	[RefreshDate] [datetime] NOT NULL,
 CONSTRAINT [PK_MarketDataCredentials] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


