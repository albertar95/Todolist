USE [ToDoListDb]
GO

/****** Object:  Table [dbo].[Transactions]    Script Date: 7/4/2024 12:58:26 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Transactions](
	[NidTransaction] [uniqueidentifier] NOT NULL,
	[TransactionType] [tinyint] NOT NULL,
	[PayerAccount] [uniqueidentifier] NOT NULL,
	[RecieverAccount] [uniqueidentifier] NOT NULL,
	[Amount] [decimal](18, 0) NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[TransactionReason] [nvarchar](max) NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[TransactionGroupId] [uniqueidentifier] NULL,
 CONSTRAINT [PK_Transactions] PRIMARY KEY CLUSTERED 
(
	[NidTransaction] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Transactions]  WITH CHECK ADD  CONSTRAINT [FK_Transactions_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([NidUser])
GO

ALTER TABLE [dbo].[Transactions] CHECK CONSTRAINT [FK_Transactions_Users]
GO


