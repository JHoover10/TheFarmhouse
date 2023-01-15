-------------------------------------------------------------------------------
-- Created      1/15/2023
-- Purpose      Script will turn table data into upsert statements.
--              Just replace the table name placeholder with your table
--              of choice, run it, and copy out the results.
-------------------------------------------------------------------------------
-- Modification History
--
-- 01/15/2023: Created
-------------------------------------------------------------------------------


DECLARE @tableName varchar(max) = '{YOUR_TABLE_NAME_HERE}'
DECLARE @counter int = (SELECT COUNT(*) FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id WHERE t.name = @tableName)

DECLARE @whereClause varchar(max) = ''
DECLARE @updateSet varchar(max) = ''
DECLARE @insertColumns varchar(max) = ''
DECLARE @insertValues varchar(max) = ''
DECLARE @formatedColumnName varchar(max) = ''

WHILE @counter > 0
BEGIN
	DECLARE @columnId bigint = 
		(SELECT c.column_id FROM sys.tables t 
		JOIN sys.columns c ON c.object_id = t.object_id 
		WHERE t.name = @tableName 
		ORDER BY c.column_id DESC 
		OFFSET @counter - 1 ROWS FETCH NEXT (1) ROWS ONLY)
	
	DECLARE @isPrimaryKey bit = 
		(SELECT CAST(COUNT(*) AS bit)
		FROM sys.tables t
		JOIN sys.indexes i ON i.object_id = t.object_id AND i.is_primary_key = 1
		JOIN sys.index_columns ic ON ic.object_id = t.object_id AND ic.index_id = i.index_id
		JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = ic.column_id
		WHERE t.name = @tableName AND c.column_id = @columnId)
	
	DECLARE @dataType varchar(max) = 
		(SELECT ty.name
		FROM sys.tables t
		LEFT JOIN sys.columns c ON c.object_id = t.object_id
		LEFT JOIN sys.types ty on ty.user_type_id = c.user_type_id
		WHERE t.name = @tableName AND c.column_id = @columnId)

	DECLARE @columnName varchar(max) = 
		(SELECT c.name FROM sys.tables t 
		JOIN sys.columns c ON c.object_id = t.object_id 
		WHERE t.name = @tableName AND c.column_id = @columnId)
	
	SET @formatedColumnName =
		CASE
			WHEN @dataType in ('bit', 'tinyint', 'smallint', 'int', 'bigint', 'decimal', 'numeric', 'smallmoney', 'money', 'float', 'real') THEN 'ISNULL(CAST(' + @columnName + ' as varchar), ''NULL'')'
			WHEN @dataType in ('char', 'varchar', 'text', 'nchar', 'nvarchar', 'ntext', 'binary', 'varbinary', 'image') THEN ''''''''' + ' + @columnName + ' + '''''''''
			WHEN @dataType in ('datetime', 'datetime2', 'smalldatetime', 'date', 'time', 'datetimeoffset', 'timestamp', 'uniqueidentifier') THEN ''''''''' + ISNULL(CAST(' + @columnName + ' as varchar), ''NULL'') + '''''''''
		END

	IF (@isPrimaryKey = 1)
	BEGIN	
		SET @whereClause = @whereClause + @columnName + ' = '' + ' + @formatedColumnName + ' + ''''AND '
	END
	ELSE
	BEGIN
		SET @updateSet = @updateSet + @columnName + ' = '' + ' + @formatedColumnName + ' + '', '
		SET @insertColumns = @insertColumns + @columnName + ', '
		Set @insertValues = @insertValues + ''' + ' + @formatedColumnName + ' + '', '
	END

	SET @counter = @counter - 1
END

SET @whereClause = LEFT(@whereClause, LEN(@whereClause) - 6)
SET @updateSet = LEFT(@updateSet, LEN(@updateSet) - 3)
SET @insertColumns = LEFT(@insertColumns, LEN(@insertColumns) - 1)
SET @insertValues = LEFT(@insertValues, LEN(@insertValues) - 1)

DECLARE @query varchar(max) =
'
IF EXISTS (SELECT * FROM ' + @tableName + ' WHERE ' + @whereClause + ' '')
BEGIN
    UPDATE ' + @tableName + ' SET ' + @updateSet + ''' WHERE ' + @whereClause + '''
END
ELSE
BEGIN
    INSERT ' + @tableName + ' (' + @insertColumns + ') VALUES (' + @insertValues + ')
END
'

DECLARE @finalQuery nvarchar(max) = 'SELECT ''' + @query + ''' FROM ' + @tableName

DECLARE @upsertStatements AS TABLE (UpsertStatement varchar(max)) 
INSERT INTO @upsertStatements EXECUTE sp_executesql @finalQuery

SELECT REPLACE(UpsertStatement, '''NULL''', 'NULL') FROM @upsertStatements