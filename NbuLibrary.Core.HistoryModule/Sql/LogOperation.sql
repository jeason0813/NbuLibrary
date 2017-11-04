CREATE PROC [_History_LogOperation] (
	@Entity NVARCHAR(128),
	@EntityId INT,
	@Operation TINYINT,
	@ByUser INT,
	@OnDate DATETIME,
	@Id INT OUTPUT
)
as
BEGIN
INSERT INTO [_History_EntityOperations]
           ([Entity]
           ,[EntityId]
           ,[Operation]
           ,[ByUser]
           ,[OnDate])
     VALUES
           (@Entity
           ,@EntityId
           ,@Operation
           ,@ByUser
           ,@OnDate);
    SET @Id = SCOPE_IDENTITY();
END