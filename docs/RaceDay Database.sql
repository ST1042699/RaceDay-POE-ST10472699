--1. Database Created
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RaceDay')
BEGIN
    CREATE DATABASE RaceDay;
END
GO

USE RaceDay;
GO

--2. created to make the database clean
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.[User]', 'U') IS NOT NULL DROP TABLE dbo.[User];
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

--3. Roles Table created 
CREATE TABLE Roles (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    Rolename VARCHAR(50) NOT NULL UNIQUE
);

--4. User Table created 
CREATE TABLE [User] (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    RoleID INT NOT NULL,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_User_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);

--5. Events table created 
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

--6. Categories Table created 
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName VARCHAR(50) NOT NULL,
    DistanceKM DECIMAL(5,2) NOT NULL,
    EntryFee DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID) REFERENCES Events(EventID) ON DELETE CASCADE
);

--7.Enrolments Table created
CREATE TABLE Enrolments (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolledAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Enrolments_User FOREIGN KEY (ParticipantID) REFERENCES [User](UserID),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID),
    CONSTRAINT UQ_Participant_Category UNIQUE (ParticipantID, CategoryID)
);

--8. Results Table Created 
CREATE TABLE Results (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    CompletionTime TIME NOT NULL,
    RankPosition INT NOT NULL,
    RecordedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID)
);
GO

-- Roles Data
INSERT INTO Roles (Rolename) VALUES 
('Organiser'),
('Participant');

-- User Data 2 Organisers and 2 Partipants
INSERT INTO [User] (RoleID, FullName, Email, PasswordHash) VALUES
(1, 'Leo Smith', 'Leo.organiser@raceday.com', 'hashed_pass_167'),
(1, 'Sarah Sasha', 'sarah.organiser@raceday.com', 'hashed_pass_456'),
(2, 'Michael Jordan', 'michael.runner@gmail.com', 'hashed_pass_789'),
(2, 'Jordan Stone', 'Jordan.runner@gmail.com', 'hashed_pass_100');

-- Event data 3 events 
INSERT INTO Events (OrganiserID, Title, Description, EventDate, Location) VALUES
(1, 'Marathon 2026', 'Annual road running event.', '2026-11-01 06:00:00', 'FNB Stadium, Johannesburg'),
(1, 'Cape Town Cycle Tour 2026', 'Scenic coastal cycling event.', '2026-10-15 07:00:00', 'Cape Town City Centre'),
(2, 'Durban Ultra Tri 2026', 'Premier triathlon along the Golden Mile.', '2026-12-05 06:30:00', 'Suncoast Beach, Durban');

--Categories data
INSERT INTO Categories (EventID, CategoryName, DistanceKM, EntryFee) VALUES
(1, 'Full Marathon', 42.20, 350.00),
(1, 'Half Marathon', 21.10, 250.00),
(2, 'Full Loop Cycle', 109.00, 550.00),
(2, 'Short Route Cycle', 42.00, 300.00),
(3, 'Distance Tri', 113.00, 1200.00);

--Enrolment Data
INSERT INTO Enrolments (ParticipantID, CategoryID) VALUES
(3, 1), 
(4, 2),
(3, 3),
(4, 5);

--Result data
INSERT INTO Results (EnrolmentID, CompletionTime, RankPosition) VALUES
(1, '03:45:12', 12),
(2, '01:52:30', 5);
GO

SELECT * FROM Roles;
SELECT * FROM [User];
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM Enrolments;
SELECT * FROM Results;