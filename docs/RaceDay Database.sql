--1. create the database 
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RaceDay')
BEGIN
    CREATE DATABASE RaceDay;
END
GO

USE RaceDay;
GO

--2. created a clean up 
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.[User]', 'U') IS NOT NULL DROP TABLE dbo.[User];
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

--3. created the first table called roles 
CREATE TABLE Roles (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    Rolename VARCHAR(50) NOT NULL UNIQUE
);

-- 4.table called user
CREATE TABLE [User] (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    RoleID INT NOT NULL,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_User_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);

--5. create table events
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