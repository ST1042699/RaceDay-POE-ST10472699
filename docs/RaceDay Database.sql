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