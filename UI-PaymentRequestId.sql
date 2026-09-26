BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260925044137_UI_PaymentRequestId'
)
BEGIN
    ALTER TABLE [FeePayments] ADD [RequestId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260925044137_UI_PaymentRequestId'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_FeePayments_SchoolId_RequestId] ON [FeePayments] ([SchoolId], [RequestId]) WHERE [RequestId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260925044137_UI_PaymentRequestId'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260925044137_UI_PaymentRequestId', N'9.0.0');
END;

COMMIT;
GO

