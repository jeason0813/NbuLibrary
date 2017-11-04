/*Script Configuration Section
================================*/
DECLARE @v_Schema nvarchar(20) = 'dbo';
---------------------------------------


/*Declarations
=======================*/

DECLARE @v_ConstName nvarchar(100), 
		@v_TableID int,
		@v_TableName nvarchar(100),
		@v_ViewName nvarchar(100),
		@v_ProcName nvarchar(100),
		@v_FuncName	nvarchar(100),
		@v_TypeName	nvarchar(100);

------------------------------------------

/*Clear foreign keys
=======================*/
DECLARE crsConst CURSOR FAST_FORWARD FOR
	SELECT name, parent_object_id FROM sys.all_objects
	WHERE schema_id = schema_id(@v_Schema) 
		AND TYPE = 'F'
BEGIN TRY
	OPEN crsConst;
	
	FETCH NEXT FROM crsConst
	INTO @v_ConstName, @v_TableID;
	
	WHILE @@FETCH_STATUS = 0
	BEGIN
		SELECT @v_TableName = name
		FROM sys.all_objects
		WHERE OBJECT_ID = @v_TableID
		
		print ('ALTER TABLE ' + @v_Schema + '.['  + @v_TableName  + '] DROP CONSTRAINT ' + @v_ConstName);
		exec ('ALTER TABLE ' + @v_Schema + '.['  + @v_TableName  + '] DROP CONSTRAINT ' + @v_ConstName);
		
		FETCH NEXT FROM crsConst
		INTO @v_ConstName, @v_TableID;
	END
	
	CLOSE crsConst;
	DEALLOCATE crsConst;
END TRY
BEGIN CATCH
	DECLARE @crsConstStatus int = CURSOR_STATUS('global', 'crsConst');

	IF (@crsConstStatus = 1)
	BEGIN
		CLOSE crsConst;
		DEALLOCATE crsConst;
	END
	
	print ERROR_MESSAGE();
END CATCH	
-----------------------------------------------

/*Clear views
=======================*/
DECLARE crsView CURSOR FAST_FORWARD FOR
	SELECT name FROM sys.all_objects
	WHERE schema_id = schema_id(@v_Schema) AND TYPE = 'V'

BEGIN TRY
	OPEN crsView;

	FETCH NEXT FROM crsView
	INTO @v_ViewName;
	
	WHILE @@FETCH_STATUS = 0
	BEGIN		
		print ('DROP VIEW [' + @v_Schema + '].[' +  @v_ViewName + ']');
		exec ('DROP VIEW [' + @v_Schema + '].[' +  @v_ViewName + ']');
		
		FETCH NEXT FROM crsView
		INTO @v_ViewName;	
	END
	
	CLOSE crsView;
	DEALLOCATE crsView;
	
END TRY
BEGIN CATCH
	DECLARE @crsViewStatus int = CURSOR_STATUS('global', 'crsView');
	
	IF (@crsViewStatus = 1)
	BEGIN
		CLOSE crsView;
		DEALLOCATE crsView;
	END
	
	print ERROR_MESSAGE();
END CATCH
---------------------------------------------------------------------

/*Clear tables
=======================*/
DECLARE crsTables CURSOR FAST_FORWARD FOR
	SELECT name FROM sys.all_objects
	WHERE schema_id = schema_id(@v_Schema) AND TYPE = 'U'

BEGIN TRY
	OPEN crsTables;

	FETCH NEXT FROM crsTables
	INTO @v_TableName;
	
	WHILE @@FETCH_STATUS = 0
	BEGIN		
		print ('DROP TABLE [' + @v_Schema + '].[' +  @v_TableName + ']');
		exec ('DROP TABLE [' + @v_Schema + '].[' +  @v_TableName + ']');
		
		FETCH NEXT FROM crsTables
		INTO @v_TableName;	
	END
	
	CLOSE crsTables;
	DEALLOCATE crsTables;
	
END TRY
BEGIN CATCH
	DECLARE @crsTableStatus int = CURSOR_STATUS('global', 'crsTables');
	
	IF (@crsTableStatus = 1)
	BEGIN
		CLOSE crsTables;
		DEALLOCATE crsTables;
	END
	
	print ERROR_MESSAGE();
END CATCH
---------------------------------------------------------------------

/*Clear stored procedures
=======================*/
DECLARE crsProcs CURSOR FAST_FORWARD FOR
	SELECT name FROM sys.all_objects
	WHERE schema_id = schema_id(@v_Schema) AND TYPE = 'P'

BEGIN TRY
	OPEN crsProcs;

	FETCH NEXT FROM crsProcs
	INTO @v_ProcName;
	
	WHILE @@FETCH_STATUS = 0
	BEGIN
		print ('DROP PROCEDURE ' + @v_Schema + '.' +  @v_ProcName);		
		exec ('DROP PROCEDURE ' + @v_Schema + '.' +  @v_ProcName);
		
		FETCH NEXT FROM crsProcs
		INTO @v_ProcName;	
	END
	
	CLOSE crsProcs;
	DEALLOCATE crsProcs;
	
END TRY
BEGIN CATCH
	DECLARE @crsProcStatus int = CURSOR_STATUS('global', 'crsProcs');
	
	IF (@crsProcStatus = 1)
	BEGIN
		CLOSE crsProcs;
		DEALLOCATE crsProcs;
	END
	
	print ERROR_MESSAGE();
END CATCH
----------------------------------------------------------------------


/*Clear functions
=======================*/
DECLARE crsFunc CURSOR FAST_FORWARD FOR
	SELECT name FROM sys.all_objects
	WHERE schema_id = schema_id(@v_Schema) AND (TYPE = 'FN' OR TYPE='IF' )
BEGIN TRY
	OPEN crsFunc;

	FETCH NEXT FROM crsFunc
	INTO @v_FuncName;
	
	WHILE @@FETCH_STATUS = 0
	BEGIN		
		print ('DROP FUNCTION ' + @v_Schema + '.' +  @v_FuncName);
		exec ('DROP FUNCTION ' + @v_Schema + '.' +  @v_FuncName);
		
		FETCH NEXT FROM crsFunc
		INTO @v_FuncName;	
	END
	
	CLOSE crsFunc;
	DEALLOCATE crsFunc;
	
END TRY
BEGIN CATCH
	DECLARE @crsFuncStatus int = CURSOR_STATUS('global', 'crsFunc');
	
	IF (@crsFuncStatus = 1)
	BEGIN
		CLOSE crsFunc;
		DEALLOCATE crsFunc;
	END
	
	print ERROR_MESSAGE();
END CATCH
----------------------------------------------------------------------

/*Clear types
=======================*/
DECLARE crsType CURSOR FAST_FORWARD FOR
	SELECT name FROM sys.types
	WHERE schema_id = schema_id(@v_Schema)
BEGIN TRY
	OPEN crsType;

	FETCH NEXT FROM crsType
	INTO @v_TypeName;
	
	WHILE @@FETCH_STATUS = 0
	BEGIN		
		print ('DROP TYPE ' + @v_Schema + '.' +  @v_TypeName);
		exec ('DROP TYPE ' + @v_Schema + '.' +  @v_TypeName);
		
		FETCH NEXT FROM crsType
		INTO @v_TypeName;	
	END
	
	CLOSE crsType;
	DEALLOCATE crsType;
	
END TRY
BEGIN CATCH
	DECLARE @crsTypeStatus int = CURSOR_STATUS('global', 'crsType');
	
	IF (@crsTypeStatus = 1)
	BEGIN
		CLOSE crsType;
		DEALLOCATE crsType;
	END
	
	print ERROR_MESSAGE();
END CATCH
----------------------------------------------------------------------