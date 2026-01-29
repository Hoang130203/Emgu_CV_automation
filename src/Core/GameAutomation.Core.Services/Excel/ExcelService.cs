using ExcelDataReader;
using System.Data;
using System.Text;

namespace GameAutomation.Core.Services.Excel;

/// <summary>
/// Excel service implementation using ExcelDataReader (free, read-only)
/// </summary>
public class ExcelService : IExcelService
{
    static ExcelService()
    {
        // Required for ExcelDataReader to work with .NET Core
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Reads data from an Excel file
    /// </summary>
    public Task<List<Dictionary<string, object>>> ReadAsync(string filePath, string? sheetName = null)
    {
        var result = new List<Dictionary<string, object>>();

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = true
            }
        });

        var table = string.IsNullOrEmpty(sheetName)
            ? dataSet.Tables[0]
            : dataSet.Tables[sheetName];

        if (table == null)
            return Task.FromResult(result);

        foreach (DataRow row in table.Rows)
        {
            var rowData = new Dictionary<string, object>();
            foreach (DataColumn col in table.Columns)
            {
                rowData[col.ColumnName] = row[col] ?? string.Empty;
            }
            result.Add(rowData);
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Reads a specific row from Excel file by row number (1-indexed data row, after header)
    /// Row 1 = first data row (row 2 in Excel if has header)
    /// </summary>
    public (string username, string password)? ReadCredentials(
        string filePath,
        string? sheetName = null,
        int dataRowNumber = 1)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[ExcelService] File not found: {filePath}");
            return null;
        }

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = false // Read raw to get exact row
            }
        });

        var table = string.IsNullOrEmpty(sheetName)
            ? dataSet.Tables[0]
            : dataSet.Tables[sheetName];

        if (table == null)
        {
            Console.WriteLine($"[ExcelService] Sheet not found: {sheetName ?? "first sheet"}");
            return null;
        }

        Console.WriteLine($"[ExcelService] Total rows in table (including header): {table.Rows.Count}");

        // dataRowNumber is 1-indexed (row 1 = first data row after header = Excel row 2)
        // table.Rows[0] = header, table.Rows[1] = first data row, etc.
        if (dataRowNumber < 1 || dataRowNumber >= table.Rows.Count)
        {
            Console.WriteLine($"[ExcelService] Row {dataRowNumber} out of range (valid: 1 to {table.Rows.Count - 1})");
            return null;
        }

        var row = table.Rows[dataRowNumber]; // Index dataRowNumber = Excel row (dataRowNumber + 1)

        var username = row[0]?.ToString()?.Trim() ?? string.Empty;
        var password = row[1]?.ToString()?.Trim() ?? string.Empty;

        Console.WriteLine($"[ExcelService] Row {dataRowNumber}: username='{username}', password='{(string.IsNullOrEmpty(password) ? "(empty)" : "***")}'");

        if (string.IsNullOrEmpty(username))
        {
            Console.WriteLine($"[ExcelService] Username is empty at row {dataRowNumber}");
            return null;
        }

        return (username, password);
    }

    /// <summary>
    /// Gets total data row count (excluding header)
    /// </summary>
    public int GetRowCount(string filePath, string? sheetName = null)
    {
        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var dataSet = reader.AsDataSet();

        var table = string.IsNullOrEmpty(sheetName)
            ? dataSet.Tables[0]
            : dataSet.Tables[sheetName];

        if (table == null)
            return 0;

        return Math.Max(0, table.Rows.Count - 1); // Subtract header row
    }

    /// <summary>
    /// Gets list of sheet names in Excel file
    /// </summary>
    public List<string> GetSheetNames(string filePath)
    {
        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var dataSet = reader.AsDataSet();
        var names = new List<string>();

        foreach (DataTable table in dataSet.Tables)
        {
            names.Add(table.TableName);
        }

        return names;
    }

    // Not implemented - we only need read functionality
    public Task WriteAsync(string filePath, List<Dictionary<string, object>> data, string? sheetName = null)
        => throw new NotImplementedException("Write not supported - use read-only mode");

    public Task AppendAsync(string filePath, Dictionary<string, object> row, string? sheetName = null)
        => throw new NotImplementedException("Append not supported - use read-only mode");
}
