CREATE DATABASE dude
GO

CREATE TABLE dbo.NosePicker (
	NosePickerID	int				NOT NULL PRIMARY KEY,
	FirstName		nvarchar(50)	NOT NULL,
	LastName		nvarchar(50)	NOT NULL,
)
GO

INSERT INTO dbo.NosePicker (NosePickerID, FirstName, LastName)
VALUES	(1, 'Alan', 'West'),
		(2, 'Code', 'Blanch')
GO

CREATE TABLE dbo.Booger (
	BoogerID		int				NOT NULL PRIMARY KEY IDENTITY,
	NosePickerID	int				NOT NULL REFERENCES dbo.NosePicker (NosePickerID),
	Color			varchar(10)		NOT NULL
)
GO

INSERT INTO dbo.Booger (NosePickerID, Color)
VALUES	(1, 'Green'),
		(2, 'Red'),
		(2, 'Blue')
GO
