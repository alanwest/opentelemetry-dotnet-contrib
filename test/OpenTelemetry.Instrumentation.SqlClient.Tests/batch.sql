CREATE TABLE dbo.Users (
  UserID int IDENTITY(1,1) PRIMARY KEY,
  [Name] nvarchar(100)
);

INSERT INTO dbo.Users ([Name]) VALUES ('Pat');
