namespace GameAutomation.Core.Services.Excel;

/// <summary>
/// Interface for Excel file operations
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// Reads data from an Excel file
    /// </summary>
    Task<List<Dictionary<string, object>>> ReadAsync(string filePath, string? sheetName = null);

    /// <summary>
    /// Writes data to an Excel file
    /// </summary>
    Task WriteAsync(string filePath, List<Dictionary<string, object>> data, string? sheetName = null);

    /// <summary>
    /// Appends data to an existing Excel file
    /// </summary>
    Task AppendAsync(string filePath, Dictionary<string, object> row, string? sheetName = null);
}
