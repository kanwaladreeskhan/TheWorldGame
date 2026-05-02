-- =========================================

CREATE DATABASE db;
GO

USE db;
GO

-- =========================
-- COUNTRIES
-- =========================
CREATE TABLE Countries (
    CountryId INT PRIMARY KEY,
    Name NVARCHAR(50) UNIQUE NOT NULL,
    StrategyType NVARCHAR(20) CHECK (StrategyType IN ('Aggressive','Balanced','Conservative')),
    GDP FLOAT,
    Population INT
);

-- =========================
-- PLAYERS
-- =========================
CREATE TABLE Players (
    PlayerId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    CountryId INT,
    Balance FLOAT CHECK (Balance >= 0),
    FOREIGN KEY (CountryId) REFERENCES Countries(CountryId)
);

-- =========================
-- RESOURCES
-- =========================
CREATE TABLE Resources (
    ResourceId INT PRIMARY KEY,
    Name NVARCHAR(50) UNIQUE NOT NULL,
    BasePrice FLOAT CHECK (BasePrice > 0),
    Category NVARCHAR(50)
);

-- =========================
-- MARKET PRICES
-- =========================
CREATE TABLE MarketPrices (
    ResourceId INT PRIMARY KEY,
    CurrentPrice FLOAT CHECK (CurrentPrice > 0),
    Demand INT DEFAULT 0,
    Supply INT DEFAULT 0,
    FOREIGN KEY (ResourceId) REFERENCES Resources(ResourceId)
);

-- =========================
-- PLAYER RESOURCES
-- =========================
CREATE TABLE PlayerResources (
    PlayerId INT,
    ResourceId INT,
    Quantity INT CHECK (Quantity >= 0),
    PRIMARY KEY (PlayerId, ResourceId),
    FOREIGN KEY (PlayerId) REFERENCES Players(PlayerId),
    FOREIGN KEY (ResourceId) REFERENCES Resources(ResourceId)
);

-- =========================
-- TRADES
-- =========================
CREATE TABLE Trades (
    TradeId INT IDENTITY(1,1) PRIMARY KEY,
    PlayerId INT,
    ResourceId INT,
    TradeType NVARCHAR(10) CHECK (TradeType IN ('BUY','SELL')),
    Quantity INT CHECK (Quantity > 0),
    PriceAtTrade FLOAT CHECK (PriceAtTrade > 0),
    TradeDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (PlayerId) REFERENCES Players(PlayerId),
    FOREIGN KEY (ResourceId) REFERENCES Resources(ResourceId)
);

-- =========================
-- AI RULES
-- =========================
CREATE TABLE AIRules (
    RuleId INT IDENTITY(1,1) PRIMARY KEY,
    ResourceId INT,
    ConditionType NVARCHAR(20),
    Threshold INT,
    Action NVARCHAR(10),
    MinBalance FLOAT,
    MaxQuantity INT,
    FOREIGN KEY (ResourceId) REFERENCES Resources(ResourceId)
);

-- =========================
-- TRADE LOGS
-- =========================
CREATE TABLE TradeLogs (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    Message NVARCHAR(200),
    LogDate DATETIME DEFAULT GETDATE()
);

-- =========================================
-- INSERT DATA
-- =========================================

INSERT INTO Countries VALUES
(1,'USA','Aggressive',21000,330),
(2,'China','Balanced',17000,1400),
(3,'Germany','Conservative',4000,83),
(4,'Japan','Balanced',5000,125),
(5,'India','Aggressive',3500,1380),
(6,'UK','Conservative',3000,67),
(7,'Russia','Aggressive',1800,146),
(8,'UAE','Balanced',500,10);

INSERT INTO Resources VALUES
(1,'Oil',100,'Energy'),
(2,'Gold',500,'Precious'),
(3,'Food',20,'Essential'),
(4,'Steel',150,'Industrial'),
(5,'Technology',800,'Advanced'),
(6,'Gas',90,'Energy'),
(7,'Coal',60,'Energy'),
(8,'Electronics',700,'Advanced');

INSERT INTO MarketPrices VALUES
(1,110,500,400),
(2,520,200,150),
(3,25,800,900),
(4,160,300,250),
(5,850,150,100),
(6,95,400,350),
(7,70,450,500),
(8,750,200,180);

INSERT INTO Players (Name, CountryId, Balance) VALUES
('Player1',1,5000),
('ChinaAI',2,7000),
('GermanyAI',3,6000),
('JapanAI',4,6500),
('IndiaAI',5,5500),
('UKAI',6,5000),
('RussiaAI',7,7500),
('UAEAI',8,8000);

INSERT INTO PlayerResources VALUES
(1,1,10),(1,2,5),(1,3,20),(1,4,8),(1,5,2),(1,6,6),(1,7,10),(1,8,1),
(2,1,30),(2,2,10),(2,3,40),(2,4,25),(2,5,5),(2,6,20),(2,7,30),(2,8,10),
(3,1,15),(3,2,20),(3,3,25),(3,4,30),(3,5,10),(3,6,8),(3,7,12),(3,8,15),
(4,1,10),(4,2,15),(4,3,20),(4,4,12),(4,5,25),(4,6,10),(4,7,5),(4,8,20),
(5,1,20),(5,2,5),(5,3,50),(5,4,15),(5,5,3),(5,6,12),(5,7,18),(5,8,6),
(6,1,12),(6,2,18),(6,3,22),(6,4,10),(6,5,8),(6,6,9),(6,7,11),(6,8,14),
(7,1,50),(7,2,8),(7,3,15),(7,4,35),(7,5,2),(7,6,25),(7,7,40),(7,8,5),
(8,1,40),(8,2,12),(8,3,10),(8,4,8),(8,5,6),(8,6,15),(8,7,20),(8,8,7);

-- =========================================
-- VIEWS
-- =========================================

CREATE VIEW Leaderboard AS
SELECT p.PlayerId,
       p.Name,
       p.Balance + SUM(pr.Quantity * mp.CurrentPrice) AS TotalWealth
FROM Players p
JOIN PlayerResources pr ON p.PlayerId = pr.PlayerId
JOIN MarketPrices mp ON pr.ResourceId = mp.ResourceId
GROUP BY p.PlayerId, p.Name, p.Balance;

CREATE VIEW ResourceDemand AS
SELECT r.ResourceId,
       r.Name,
       mp.Demand,
       mp.Supply,
       mp.CurrentPrice
FROM Resources r
JOIN MarketPrices mp ON r.ResourceId = mp.ResourceId;

-- =========================================
-- MARKET PRICE UPDATE PROCEDURE
-- Formula:
-- NewPrice = BasePrice + ((Demand - Supply) * 0.5)
-- =========================================

CREATE PROCEDURE UpdateMarketPrice
@ResourceId INT
AS
BEGIN
    DECLARE @BasePrice FLOAT;
    DECLARE @Demand INT;
    DECLARE @Supply INT;
    DECLARE @NewPrice FLOAT;

    SELECT @BasePrice = r.BasePrice,
           @Demand = m.Demand,
           @Supply = m.Supply
    FROM Resources r
    JOIN MarketPrices m ON r.ResourceId = m.ResourceId
    WHERE r.ResourceId = @ResourceId;

    SET @NewPrice = @BasePrice + ((@Demand - @Supply) * 0.5);

    IF @NewPrice < 1
        SET @NewPrice = 1;

    UPDATE MarketPrices
    SET CurrentPrice = @NewPrice
    WHERE ResourceId = @ResourceId;
END;
GO

-- =========================================
-- BUY RESOURCE PROCEDURE
-- =========================================

CREATE PROCEDURE BuyResource
@PlayerId INT,
@ResourceId INT,
@Qty INT
AS
BEGIN
    DECLARE @Price FLOAT;
    DECLARE @Balance FLOAT;

    IF @Qty <= 0
    BEGIN
        PRINT 'Invalid Quantity';
        RETURN;
    END

    SELECT @Price = CurrentPrice FROM MarketPrices WHERE ResourceId = @ResourceId;
    SELECT @Balance = Balance FROM Players WHERE PlayerId = @PlayerId;

    IF @Balance < (@Price * @Qty)
    BEGIN
        PRINT 'Insufficient Balance';
        RETURN;
    END

    UPDATE Players
    SET Balance = Balance - (@Price * @Qty)
    WHERE PlayerId = @PlayerId;

    IF EXISTS (SELECT * FROM PlayerResources WHERE PlayerId=@PlayerId AND ResourceId=@ResourceId)
    BEGIN
        UPDATE PlayerResources
        SET Quantity = Quantity + @Qty
        WHERE PlayerId=@PlayerId AND ResourceId=@ResourceId;
    END
    ELSE
    BEGIN
        INSERT INTO PlayerResources(PlayerId, ResourceId, Quantity)
        VALUES(@PlayerId, @ResourceId, @Qty);
    END

    UPDATE MarketPrices
    SET Demand = Demand + @Qty
    WHERE ResourceId = @ResourceId;

    INSERT INTO Trades(PlayerId, ResourceId, TradeType, Quantity, PriceAtTrade)
    VALUES(@PlayerId, @ResourceId, 'BUY', @Qty, @Price);

    EXEC UpdateMarketPrice @ResourceId;
END;
GO

-- =========================================
-- SELL RESOURCE PROCEDURE
-- =========================================

CREATE PROCEDURE SellResource
@PlayerId INT,
@ResourceId INT,
@Qty INT
AS
BEGIN
    DECLARE @Price FLOAT;
    DECLARE @Owned INT;

    IF @Qty <= 0
    BEGIN
        PRINT 'Invalid Quantity';
        RETURN;
    END

    SELECT @Price = CurrentPrice FROM MarketPrices WHERE ResourceId = @ResourceId;

    SELECT @Owned = Quantity
    FROM PlayerResources
    WHERE PlayerId=@PlayerId AND ResourceId=@ResourceId;

    IF @Owned IS NULL OR @Owned < @Qty
    BEGIN
        PRINT 'Not enough resource';
        RETURN;
    END

    UPDATE Players
    SET Balance = Balance + (@Price * @Qty)
    WHERE PlayerId=@PlayerId;

    UPDATE PlayerResources
    SET Quantity = Quantity - @Qty
    WHERE PlayerId=@PlayerId AND ResourceId=@ResourceId;

    UPDATE MarketPrices
    SET Supply = Supply + @Qty
    WHERE ResourceId=@ResourceId;

    INSERT INTO Trades(PlayerId, ResourceId, TradeType, Quantity, PriceAtTrade)
    VALUES(@PlayerId, @ResourceId, 'SELL', @Qty, @Price);

    EXEC UpdateMarketPrice @ResourceId;
END;
GO

-- =========================================
-- TRIGGER
-- =========================================

CREATE TRIGGER trg_LogTrade
ON Trades
AFTER INSERT
AS
BEGIN
    INSERT INTO TradeLogs (Message)
    SELECT 'Trade executed: Player ' + CAST(PlayerId AS NVARCHAR(10)) +
           ' | Resource ' + CAST(ResourceId AS NVARCHAR(10)) +
           ' | Type ' + TradeType
    FROM inserted;
END;
GO

-- =========================================
-- INDEXES
-- =========================================

CREATE INDEX idx_player_balance ON Players(Balance);
CREATE INDEX idx_trade_player ON Trades(PlayerId);
CREATE INDEX idx_resource_player ON PlayerResources(ResourceId);

-- =========================================
-- SAMPLE TRADES
-- =========================================

EXEC BuyResource 1,1,5;
EXEC SellResource 2,2,3;
EXEC BuyResource 3,4,10;
EXEC BuyResource 4,5,2;
EXEC SellResource 5,3,10;


