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
    IsActive BIT DEFAULT 0
);

INSERT INTO WarEvents (EventId, EventName, Description, AffectedResourceId, PriceMultiplier, SupplyDrop, DemandBoost, IsActive)
VALUES 
(1, 'Oil Crisis', 'Supply lines cut due to naval blockade!', 1, 1.5, 50, 20, 0),
(2, 'Cyber Warfare', 'Tech resources are being diverted to defense.', 2, 1.3, 30, 10, 0);