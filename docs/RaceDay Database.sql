
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RaceDay')
BEGIN
    CREATE DATABASE RaceDay;
END
GO

USE RaceDay;
GO


IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.[User]', 'U') IS NOT NULL DROP TABLE dbo.[User];
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO


CREATE TABLE Roles (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    Rolename VARCHAR(50) NOT NULL UNIQUE
);


CREATE TABLE [User] (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    RoleID INT NOT NULL,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_User_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);


CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    Title VARCHAR(100) NOT NULL,
    Description VARCHAR(MAX) NULL,
    EventDate DATETIME NOT NULL,
    Location VARCHAR(150) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_User FOREIGN KEY (OrganiserID) REFERENCES [User](UserID)
);

CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName VARCHAR(50) NOT NULL,
    DistanceKM DECIMAL(5,2) NOT NULL,
    EntryFee DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID) REFERENCES Events(EventID) ON DELETE CASCADE
);

CREATE TABLE Enrolments (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolledAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Enrolments_User FOREIGN KEY (ParticipantID) REFERENCES [User](UserID),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID),
    CONSTRAINT UQ_Participant_Category UNIQUE (ParticipantID, CategoryID)
);

CREATE TABLE Results (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    CompletionTime TIME NOT NULL,
    RankPosition INT NOT NULL,
    RecordedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID)
);
GO

INSERT INTO Roles (Rolename) VALUES 
('Organiser'),
('Participant');

INSERT INTO [User] (RoleID, FullName, Email, PasswordHash) VALUES
(1, 'John Smith', 'john.organiser@raceday.com', 'hashed_pass_123'),
(1, 'Sarah Jenkins', 'sarah.organiser@raceday.com', 'hashed_pass_456'),
(2, 'Michael Brown', 'michael.runner@gmail.com', 'hashed_pass_789'),
(2, 'Emma Watson', 'emma.runner@gmail.com', 'hashed_pass_101');