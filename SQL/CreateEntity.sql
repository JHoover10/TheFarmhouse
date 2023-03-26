DECLARE @inputTableNames TABLE([Name] varchar(max))
INSERT @inputTableNames VALUES ('*')

DECLARE @snakeToCamel bit = 1
DECLARE @addDataAnnotations bit = 1

DECLARE @results TABLE(EntityCode varchar(max))
DECLARE @tableNames TABLE([Name] varchar(max))
DECLARE @tableCounter int = CASE WHEN (SELECT TOP 1 * FROM @inputTableNames) = '*' THEN (SELECT COUNT(*) FROM sys.tables) ELSE (SELECT COUNT(*) FROM @inputTableNames) END

IF ((SELECT TOP 1 * FROM @inputTableNames) = '*')
BEGIN
	INSERT INTO @tableNames
	SELECT [Name]
	FROM sys.tables
END
ELSE
BEGIN
	INSERT INTO @tableNames
	SELECT [Name]
	FROM @inputTableNames
END

WHILE @tableCounter > 0
BEGIN
	DECLARE @properties varchar(max) = ''
	DECLARE @csharpDataType varchar(max) = ''
	DECLARE @tableName varchar(max) = (SELECT [Name] FROM @tableNames ORDER BY 1 ASC OFFSET @tableCounter - 1 ROWS FETCH NEXT (1) ROWS ONLY)
	DECLARE @columnCounter int = (SELECT COUNT(*) FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id WHERE t.name = @tableName)

	WHILE @columnCounter > 0
	BEGIN
		DECLARE @columnId bigint = 
			(SELECT c.column_id FROM sys.tables t 
			JOIN sys.columns c ON c.object_id = t.object_id 
			WHERE t.name = @tableName 
			ORDER BY c.column_id DESC 
			OFFSET @columnCounter - 1 ROWS FETCH NEXT (1) ROWS ONLY)
	
		DECLARE @isPrimaryKey bit = 
			(SELECT CAST(COUNT(*) AS bit)
			FROM sys.tables t
			JOIN sys.indexes i ON i.object_id = t.object_id AND i.is_primary_key = 1
			JOIN sys.index_columns ic ON ic.object_id = t.object_id AND ic.index_id = i.index_id
			JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = ic.column_id
			WHERE t.name = @tableName AND c.column_id = @columnId)
	
		DECLARE @isUniqueConstraint bit = 
			(SELECT CAST(COUNT(*) AS bit)
			FROM sys.tables t
			JOIN sys.indexes i ON i.object_id = t.object_id AND i.is_primary_key = 0 AND i.is_unique = 1
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
	
		SET @csharpDataType =
			CASE
				WHEN @dataType = 'bit' THEN 'bool'
				WHEN @dataType = 'tinyint' THEN 'short'
				WHEN @dataType = 'smallint' THEN 'short'
				WHEN @dataType = 'int' THEN 'int'
				WHEN @dataType = 'bigint' THEN 'long'
				WHEN @dataType = 'decimal' THEN 'decimal'
				WHEN @dataType = 'numeric' THEN 'decimal'
				WHEN @dataType = 'smallmoney' THEN 'decimal'
				WHEN @dataType = 'money' THEN 'decimal'
				WHEN @dataType = 'float' THEN 'float'
				WHEN @dataType = 'real' THEN 'float'
				WHEN @dataType in ('char', 'varchar', 'text', 'nchar', 'nvarchar', 'ntext') THEN 'string'
				WHEN @dataType = 'date' THEN 'DateOnly'
				WHEN @dataType = 'time' THEN 'TimeSpan'
				WHEN @dataType = 'timestamp' THEN 'TimeSpan'
				WHEN @dataType in ('datetime', 'datetime2', 'smalldatetime', 'datetimeoffset') THEN 'DateTime'
				WHEN @dataType = 'uniqueidentifier' THEN 'Guid'
			END

		IF (@addDataAnnotations = 1)
		BEGIN
			IF (@isPrimaryKey = 1)
			BEGIN
				SET @properties = @properties + char(9) + '[Key]' + char(13) + char(10)
			END

			IF (@isUniqueConstraint = 1)
			BEGIN
				SET @properties = @properties + char(9) + '[Unique]' + char(13) + char(10)
			END
		
			IF (@dataType in ('char', 'varchar', 'text', 'nchar', 'nvarchar', 'ntext'))
			BEGIN
				SET @properties = @properties + char(9) + '[MaxLength(' +  CAST((SELECT c.max_length FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id WHERE t.name = @tableName AND column_id = @columnId) AS varchar) + ')]' + char(13) + char(10)
			END
		END

		IF (@snakeToCamel = 1)
		BEGIN
			DECLARE @camelColumnName varchar(max) = ''
			DECLARE @i int = 1
			DECLARE @length int = len(@columnName)
			DECLARE @nextUpper bit = 1

			WHILE @i <= @length
			BEGIN
				DECLARE @c nchar(1) = substring(@columnName, @i, 1)

				IF (@c = '_')
				BEGIN
					SET @nextUpper = 1
				END
				ELSE
				BEGIN
					SET @camelColumnName = @camelColumnName + CASE WHEN @nextUpper = 1 THEN UPPER(@c) ELSE LOWER(@c) END
					SET @nextUpper = 0
				END

				SET @i = @i + 1
			END

			SET @properties = @properties + char(9) + '[ColumnName("' +  @columnName + '")]' + char(13) + char(10)
			SET @properties = @properties + char(9) + 'public ' + @csharpDataType + ' ' + @camelColumnName + ' { get; set; }' + char(13) + char(10)
		END
		ELSE
		BEGIN
			SET @properties = @properties + char(9) + 'public ' + @csharpDataType + ' ' + @columnName + ' { get; set; }' + char(13) + char(10)
		END

		SET @columnCounter = @columnCounter - 1
	END

	SET @properties = 'public class ' + @tableName + char(13) + char(10) + '{' + char(13) + char(10) + @properties + '}'

	INSERT INTO @results SELECT @properties

	SET @tableCounter = @tableCounter - 1
END

SELECT * FROM @results