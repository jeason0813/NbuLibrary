CREATE PROC [_History_LogPropertyChange](
	@OperationId INT,
	@Property nvarchar(128),
	@Value nvarchar(1024)
)
AS
BEGIN
INSERT INTO [_History_PropertyChanges]
           ([OperationId]
           ,[Property]
           ,[Value])
     VALUES
           (@OperationId
           ,@Property
           ,@Value)
       
END

