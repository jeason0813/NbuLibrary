CREATE PROC [_History_LogRelationPropertyChange](
	@RelationChangeId INT,
	@Property nvarchar(128),
	@Value nvarchar(1024)
)
AS
BEGIN
INSERT INTO [_History_RelationPropertyChanges]
           ([RelationChangeId]
           ,[Property]
           ,[Value])
     VALUES
           (@RelationChangeId
           ,@Property
           ,@Value)       
END