/* =============================================================================
   Xoa TOAN BO du lieu trong database NEXUS (giu nguyen cau truc bang), dat lai IDENTITY.
   Chi dung tren may dev, truoc khi chay lai seed.sql. Khong dung khi co du lieu that.
       sqlcmd -S .\SQLEXPRESS -E -C -b -i database\reset_data.sql
   ========================================================================== */
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO
USE NEXUS;
GO
BEGIN TRANSACTION;

-- Xoa theo thu tu con truoc cha. Connections va Orders tro lan nhau nen tam tat khoa ngoai.
DELETE FROM AuditLog;
DELETE FROM Feedback;
DELETE FROM Payments;
DELETE FROM ReplacementCharges;
DELETE FROM BillCallUsage;
DELETE FROM Bills;
DELETE FROM ConnectionEquipment;
DELETE FROM OrderRequests;
DELETE FROM OrderStatusHistory;
ALTER TABLE Orders NOCHECK CONSTRAINT FK_Orders_ExistingLandline;
ALTER TABLE Connections NOCHECK CONSTRAINT FK_Connections_Landline;
DELETE FROM Connections;
DELETE FROM Orders;
ALTER TABLE Orders WITH CHECK CHECK CONSTRAINT FK_Orders_ExistingLandline;
ALTER TABLE Connections WITH CHECK CHECK CONSTRAINT FK_Connections_Landline;
DELETE FROM PurchaseItems;
DELETE FROM Purchases;
DELETE FROM Products;
DELETE FROM Vendors;
DELETE FROM PlanCallRates;
DELETE FROM Plans;
DELETE FROM UserAccounts;
DELETE FROM Customers;
DELETE FROM Employees;
DELETE FROM RetailShops;
DELETE FROM Cities;
DELETE FROM BulkDiscounts;
DELETE FROM SystemConfig;
DELETE FROM Sequences;

-- Dat lai IDENTITY de seed luon sinh cung ma so. Bang chua tung co dong thi bo qua,
-- vi RESEED 0 tren bang do se lam dong dau tien nhan ma 0 thay vi 1.
DECLARE @t NVARCHAR(128);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR
    SELECT t.name FROM sys.tables t JOIN sys.identity_columns i ON i.object_id = t.object_id
    WHERE i.last_value IS NOT NULL;
OPEN c;
FETCH NEXT FROM c INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    DBCC CHECKIDENT (@t, RESEED, 0) WITH NO_INFOMSGS;
    FETCH NEXT FROM c INTO @t;
END
CLOSE c;
DEALLOCATE c;

COMMIT;
PRINT 'NEXUS data cleared.';
GO
