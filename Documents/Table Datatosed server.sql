USE [Pexo63Lorawan]
GO

/****** Object:  Table [dbo].[Settings]    Script Date: 4/10/2020 12:39:16 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[DataToSend](
	[Serial] [nvarchar](50) NOT NULL,
	[Description] [ntext] NULL,
	[Unit] [bit] NULL,
	[ContinueMem] [bit] NULL,
	[StopKey] [bit] NULL,
	[AutoStart] [bit] NULL,
	[Delay] [int] NULL,
	[AutoStarttime] DateTime NULL,
	[DurationDay] [int] NULL,
	[DurationHour] [int] NULL,
	[IntervalSec] [int] NULL,
	[IntervalMin] [int] NULL,
	[IntervalHour] [int] NULL,
	[IntervalSendLora] [int] NULL,
	[TimezoneId] [text] NULL,
	[HighAlarmTemp] [float] NOT NULL,
	[LowAlarmTemp] [float] NOT NULL,
	[HighAlarmHumid] [float] NULL,
	[LowAlarmHumid] [float] NULL,
	[AlarmStatus1] [bit] NOT NULL,
	[AlarmStatus2] [bit] NOT NULL,
	)
GO


