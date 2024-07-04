USE [ToDoListDb]
GO

/****** Object:  Table [dbo].[TransactionGroups]    Script Date: 7/4/2024 12:57:58 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TransactionGroups](
	[NidTransactionGroup] [uniqueidentifier] NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[PaymentType] [tinyint] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_TransactionTypes] PRIMARY KEY CLUSTERED 
(
	[NidTransactionGroup] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[TransactionGroups]  WITH CHECK ADD  CONSTRAINT [FK_TransactionTypes_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([NidUser])
GO

ALTER TABLE [dbo].[TransactionGroups] CHECK CONSTRAINT [FK_TransactionTypes_Users]
GO


