using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Data;
using Xunit;

namespace Nexus.Tests;

/// <summary>
/// Kiểm tra ánh xạ EF Core khớp với <c>database/schema.sql</c> mà không cần kết nối SQL Server.
/// Lỗi gõ sai tên bảng/cột chỉ lộ ra khi chạy thật, nên chặn ngay ở unit test.
/// </summary>
public class SchemaMappingTests
{
    /// <summary>Đọc schema.sql và trả về: tên bảng → tập tên cột.</summary>
    private static Dictionary<string, HashSet<string>> ReadSchema()
    {
        var schemaPath = FindSchemaFile();
        var sql = File.ReadAllText(schemaPath);

        var tables = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        // Mỗi khối CREATE TABLE <Ten> ( ... ); trên một dòng hoặc nhiều dòng.
        var tableRegex = new Regex(@"CREATE\s+TABLE\s+(\w+)\s*\((?<body>.*?)\n\);",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Cột: <TenCot> <KiểuDữLiệu> ...  (dòng CONSTRAINT/PRIMARY KEY không khớp vì không có kiểu).
        var columnRegex = new Regex(
            @"^\s*(\w+)\s+(?:INT|BIGINT|SMALLINT|TINYINT|BIT|CHAR|VARCHAR|NVARCHAR|NCHAR|DATE|TIME|DATETIME2|DATETIME|DECIMAL|NUMERIC|MONEY|FLOAT|REAL)\b",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        foreach (Match table in tableRegex.Matches(sql))
        {
            var name = table.Groups[1].Value;
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match column in columnRegex.Matches(table.Groups["body"].Value))
            {
                columns.Add(column.Groups[1].Value);
            }

            tables[name] = columns;
        }

        return tables;
    }

    /// <summary>Tìm schema.sql bằng cách đi ngược từ thư mục chạy test lên gốc repo.</summary>
    private static string FindSchemaFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "database", "schema.sql");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Không tìm thấy database/schema.sql từ thư mục chạy test.");
    }

    /// <summary>Dựng model EF Core mà không mở kết nối tới CSDL.</summary>
    private static NexusDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NexusDbContext>()
            .UseSqlServer("Server=(local);Database=NEXUS;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new NexusDbContext(options);
    }

    [Fact]
    public void SchemaFile_ShouldBeParsable()
    {
        var tables = ReadSchema();

        // schema.sql khai báo 25 bảng; nếu parser hỏng thì test dưới sẽ báo sai.
        Assert.Equal(25, tables.Count);
        Assert.Contains("Orders", tables.Keys);
        Assert.Contains("Connections", tables.Keys);
        Assert.Contains("Bills", tables.Keys);
    }

    [Fact]
    public void EveryMappedTable_ShouldExistInSchema()
    {
        var schema = ReadSchema();
        using var db = CreateContext();

        var missing = db.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(name => name is not null && !schema.ContainsKey(name))
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void EveryMappedColumn_ShouldExistInItsTable()
    {
        var schema = ReadSchema();
        using var db = CreateContext();

        var problems = new List<string>();

        foreach (var entity in db.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is null || !schema.TryGetValue(tableName, out var columns))
            {
                continue;
            }

            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (!columns.Contains(columnName))
                {
                    problems.Add($"{tableName}.{columnName} (thuộc tính {property.Name})");
                }
            }
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void EveryRequiredSchemaColumn_ShouldBeMappedOrHaveDefault()
    {
        // Cột NOT NULL không có DEFAULT và không phải IDENTITY thì EF phải map,
        // nếu không lệnh INSERT sẽ thất bại lúc chạy.
        var schemaPath = FindSchemaFile();
        var sql = File.ReadAllText(schemaPath);
        using var db = CreateContext();

        var entityColumns = db.Model.GetEntityTypes()
            .Where(t => t.GetTableName() is not null)
            .ToDictionary(t => t.GetTableName()!, t => t.GetProperties()
                .Select(p => p.GetColumnName()).ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

        var tableRegex = new Regex(@"CREATE\s+TABLE\s+(\w+)\s*\((?<body>.*?)\n\);",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var columnRegex = new Regex(
            @"^\s*(\w+)\s+((?:INT|BIGINT|SMALLINT|TINYINT|BIT|CHAR|VARCHAR|NVARCHAR|NCHAR|DATE|TIME|DATETIME2|DATETIME|DECIMAL|NUMERIC|MONEY|FLOAT|REAL)\b.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        var unmapped = new List<string>();

        foreach (Match table in tableRegex.Matches(sql))
        {
            var tableName = table.Groups[1].Value;
            if (!entityColumns.TryGetValue(tableName, out var mapped))
            {
                continue; // bảng chưa dùng tới trong API
            }

            foreach (Match column in columnRegex.Matches(table.Groups["body"].Value))
            {
                var columnName = column.Groups[1].Value;
                var definition = column.Groups[2].Value;

                var required = definition.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase);
                var hasDefault = definition.Contains("DEFAULT", StringComparison.OrdinalIgnoreCase);
                var isIdentity = definition.Contains("IDENTITY", StringComparison.OrdinalIgnoreCase);

                if (required && !hasDefault && !isIdentity && !mapped.Contains(columnName))
                {
                    unmapped.Add($"{tableName}.{columnName}");
                }
            }
        }

        Assert.Empty(unmapped);
    }
}