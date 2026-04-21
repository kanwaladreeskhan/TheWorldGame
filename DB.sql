 
 -- Countries
CREATE TABLE Countries (
    CountryId INT PRIMARY KEY,
    Name NVARCHAR(50) UNIQUE NOT NULL,
    StrategyType NVARCHAR(20) CHECK (StrategyType IN ('Aggressive','Balanced','Conservative')),
    GDP FLOAT,
    Population INT
);

-- Players
CREATE TABLE Players (
    PlayerId INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(50),
    CountryId INT,
    Balance FLOAT CHECK (Balance >= 0),
    FOREIGN KEY (CountryId) REFERENCES Countries(CountryId)
);

-- Resources
CREATE TABLE Resources (
    ResourceId INT PRIMARY KEY,
    Name NVARCHAR(50) UNIQUE,
    BasePrice FLOAT CHECK (BasePrice > 0),
    Category NVARCHAR(50)
);

-- MarketPrices
CREATE TABLE MarketPrices (
    ResourceId INT PRIMARY KEY,
    CurrentPrice FLOAT,
    Demand INT,
    Supply INT,
    FOREIGN KEY (ResourceId) REFERENCES Resources(ResourceId)
);

-- PlayerResources
CREATE TABLE PlayerResources (
    PlayerId INT,
    ResourceId INT,
    Quantity INT CHECK (Quantity >= 0),
    PRIMARY KEY (PlayerId, ResourceId),
    FOREIGN KEY (PlayerId) REFERENCES Players(PlayerId),
    FOREIGN KEY (ResourceId) REFERENCES Resources(ResourceId)
);

-- Trades
CREATE TABLE Trades (
    TradeId INT IDENTITY PRIMARY KEY,
    PlayerId INT,
    ResourceId INT,
    TradeType NVARCHAR(10),
    Quantity INT,
    PriceAtTrade FLOAT,
    TradeDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (PlayerId) REFERENCES Players(PlayerId)
);
ALTER TABLE Trades
ADD CONSTRAINT chk_trade_type CHECK (TradeType IN ('BUY','SELL'));

-- AI Rules
CREATE TABLE AIRules (
    RuleId INT IDENTITY PRIMARY KEY,
    ResourceId INT,
    ConditionType NVARCHAR(20),
    Threshold INT,
    Action NVARCHAR(10)
);

ALTER TABLE AIRules
ADD MinBalance FLOAT,
    MaxQuantity INT;
	
-- Logs (Trigger use)
CREATE TABLE TradeLogs (
    LogId INT IDENTITY PRIMARY KEY,
    Message NVARCHAR(200),
    LogDate DATETIME DEFAULT GETDATE()
);

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

INSERT INTO Players VALUES
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

INSERT INTO Trades (PlayerId, ResourceId, TradeType, Quantity, PriceAtTrade) VALUES
(1,1,'BUY',5,110),(2,2,'SELL',3,520),(3,4,'BUY',10,160),
(4,5,'BUY',2,850),(5,3,'SELL',10,25),(6,1,'BUY',7,110),
(7,4,'SELL',5,160),(8,2,'BUY',4,520),
(1,3,'BUY',15,25),(2,1,'SELL',10,110);


INSERT INTO AIRules (ResourceId, ConditionType, Threshold, Action, MinBalance, MaxQuantity) VALUES
(1,'LOW',10,'BUY',1000,10),
(1,'HIGH',40,'SELL',0,15),
(3,'LOW',15,'BUY',500,20),
(5,'HIGH',20,'SELL',0,5);

--view leaderboard
CREATE VIEW Leaderboard AS
SELECT p.Name,
       p.Balance + SUM(pr.Quantity * mp.CurrentPrice) AS TotalWealth
FROM Players p
JOIN PlayerResources pr ON p.PlayerId = pr.PlayerId
JOIN MarketPrices mp ON pr.ResourceId = mp.ResourceId
GROUP BY p.Name, p.Balance;

--view resource demand
CREATE VIEW ResourceDemand AS
SELECT r.Name, mp.Demand, mp.Supply
FROM MarketPrices mp
JOIN Resources r ON mp.ResourceId = r.ResourceId;

--procedure for buy
CREATE PROCEDURE BuyResource
@PlayerId INT, @ResourceId INT, @Qty INT
AS
BEGIN
    DECLARE @Price FLOAT
    SELECT @Price = CurrentPrice FROM MarketPrices WHERE ResourceId = @ResourceId

    UPDATE Players SET Balance = Balance - (@Price * @Qty)
    WHERE PlayerId = @PlayerId

    UPDATE PlayerResources
    SET Quantity = Quantity + @Qty
    WHERE PlayerId = @PlayerId AND ResourceId = @ResourceId
END;

ALTER PROCEDURE BuyResource
@PlayerId INT, @ResourceId INT, @Qty INT
AS
BEGIN
    DECLARE @Price FLOAT
    DECLARE @Balance FLOAT

    SELECT @Price = CurrentPrice FROM MarketPrices WHERE ResourceId = @ResourceId
    SELECT @Balance = Balance FROM Players WHERE PlayerId = @PlayerId

    IF @Balance < (@Price * @Qty)
    BEGIN
        PRINT 'Insufficient Balance'
        RETURN
    END

    UPDATE Players 
    SET Balance = Balance - (@Price * @Qty)
    WHERE PlayerId = @PlayerId

    UPDATE PlayerResources
    SET Quantity = Quantity + @Qty
    WHERE PlayerId = @PlayerId AND ResourceId = @ResourceId

    INSERT INTO Trades (PlayerId, ResourceId, TradeType, Quantity, PriceAtTrade)
    VALUES (@PlayerId, @ResourceId, 'BUY', @Qty, @Price)
END;
--procedure for sell
CREATE PROCEDURE SellResource
@PlayerId INT, @ResourceId INT, @Qty INT
AS
BEGIN
    DECLARE @Price FLOAT
    DECLARE @Owned INT

    SELECT @Price = CurrentPrice FROM MarketPrices WHERE ResourceId = @ResourceId
    SELECT @Owned = Quantity FROM PlayerResources 
    WHERE PlayerId = @PlayerId AND ResourceId = @ResourceId

    IF @Owned < @Qty
    BEGIN
        PRINT 'Not enough resource'
        RETURN
    END

    UPDATE Players 
    SET Balance = Balance + (@Price * @Qty)
    WHERE PlayerId = @PlayerId

    UPDATE PlayerResources
    SET Quantity = Quantity - @Qty
    WHERE PlayerId = @PlayerId AND ResourceId = @ResourceId

    INSERT INTO Trades (PlayerId, ResourceId, TradeType, Quantity, PriceAtTrade)
    VALUES (@PlayerId, @ResourceId, 'SELL', @Qty, @Price)
END;

--trigger
CREATE TRIGGER trg_LogTrade
ON Trades
AFTER INSERT
AS
BEGIN
    INSERT INTO TradeLogs (Message)
    SELECT 'Trade executed for Player ' + CAST(PlayerId AS NVARCHAR)
    FROM inserted;
END;

 ---indexes
 CREATE INDEX idx_player_balance ON Players(Balance);
CREATE INDEX idx_trade_player ON Trades(PlayerId);
CREATE INDEX idx_resource ON PlayerResources(ResourceId);
































