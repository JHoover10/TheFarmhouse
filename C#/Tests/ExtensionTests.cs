using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using C_Sharp;
using Moq;
using Xunit;

namespace Test;

public class ExtensionTests
{
    [Fact]
    public void Test()
    {
        // Arrange
        var sqlCommand = new SqlCommand();
        
        // Act
        sqlCommand.ToTempTable(new List<TestModel>() { new TestModel() });

        // Assert
        Assert.True(true);
    }
}