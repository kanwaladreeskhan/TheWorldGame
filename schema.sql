-- 1. GameState Table aur Initial Data
CREATE TABLE GameState (
    Id INT PRIMARY KEY,
    TurnNumber INT,
    Mode NVARCHAR(50)
);
INSERT INTO GameState (Id, TurnNumber, Mode) VALUES (1, 1, 'Normal');

-- 2. WarEvents Table
CREATE TABLE WarEvents (
    EventId INT PRIMARY KEY,
    EventName NVARCHAR(100),
    Description NVARCHAR(MAX),
    AffectedResourceId INT,
    PriceMultiplier FLOAT,
    SupplyDrop INT,
    DemandBoost INT,
    IsActive BIT
);