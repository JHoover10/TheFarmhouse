BEGIN TRY
    IF (@@TRANCOUNT > 0)
    BEGIN
        RAISERROR('Transaction is already running.', 17, 1)
    END

    BEGIN TRAN;

    --PUT CODE BELOW HERE

    --BUT ABOVE HERE

    COMMIT;
END TRY
BEGIN CATCH
    DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE(),
            @ErrorState int = ERROR_STATE(),
            @ErrorSeverity int = ERROR_SEVERITY()

    IF (@ErrorMessage <> 'Transaction is already running.')
        ROLLBACK;
END CATCH