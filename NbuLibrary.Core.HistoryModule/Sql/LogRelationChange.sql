CREATE PROC [_History_LogRelationChange](
	@OperationId INT,
	@Entity nvarchar(128),
	@Role nvarchar(128),
	@EntityId int,
	@RelationOperation int,
	@Id int output
)
AS
BEGIN
INSERT INTO [_History_RelationChanges]
           ([OperationId]
           ,[Entity]
           ,[Role]
           ,[EntityId]
           ,[RelationOperation])
     VALUES
           (@OperationId
           ,@Entity
           ,@Role
           ,@EntityId
           ,@RelationOperation)
    SET @Id = SCOPE_IDENTITY();
END


