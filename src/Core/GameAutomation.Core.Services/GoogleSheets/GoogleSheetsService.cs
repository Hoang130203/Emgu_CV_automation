using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GameAutomation.Core.Services.GoogleSheets;

public class GoogleSheetsService
{
    private readonly SheetsService _sheetsService;

    public GoogleSheetsService(string? credentialsPath = null)
    {
        var path = credentialsPath
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "credentials.json");

        if (!File.Exists(path))
            throw new FileNotFoundException($"Google credentials file not found: {path}");

        var credential = GoogleCredential
            .FromFile(path)
            .CreateScoped(SheetsService.Scope.Spreadsheets);

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "GameAutomation"
        });
    }

    /// <summary>
    /// Read a single cell value. Row is 1-indexed (row 1 = first row in sheet).
    /// Col is 0-indexed (0=A, 1=B, ..., 6=G).
    /// </summary>
    public async Task<string?> ReadCellAsync(string spreadsheetId, string sheetName, int row, int col)
    {
        var colLetter = ColumnIndexToLetter(col);
        var range = $"{QuoteSheetName(sheetName)}!{colLetter}{row}";

        var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
        var response = await request.ExecuteAsync();

        if (response.Values == null || response.Values.Count == 0)
            return null;

        var cellValue = response.Values[0];
        if (cellValue.Count == 0)
            return null;

        return cellValue[0]?.ToString();
    }

    /// <summary>
    /// Write a value to a single cell. Row is 1-indexed. Col is 0-indexed.
    /// </summary>
    public async Task WriteCellAsync(string spreadsheetId, string sheetName, int row, int col, string value)
    {
        var colLetter = ColumnIndexToLetter(col);
        var range = $"{QuoteSheetName(sheetName)}!{colLetter}{row}";

        var valueRange = new Google.Apis.Sheets.v4.Data.ValueRange
        {
            Values = new System.Collections.Generic.List<System.Collections.Generic.IList<object>>
            {
                new System.Collections.Generic.List<object> { value }
            }
        };

        var request = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await request.ExecuteAsync();
    }

    /// <summary>
    /// Read username (col A) and password (col B) from a row. Row is 1-indexed.
    /// </summary>
    public async Task<(string username, string password)?> ReadCredentialsAsync(
        string spreadsheetId, string sheetName, int row)
    {
        var range = $"{QuoteSheetName(sheetName)}!A{row}:B{row}";
        var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
        var response = await request.ExecuteAsync();

        if (response.Values == null || response.Values.Count == 0)
            return null;

        var rowData = response.Values[0];
        var username = rowData.Count > 0 ? rowData[0]?.ToString()?.Trim() ?? "" : "";
        var password = rowData.Count > 1 ? rowData[1]?.ToString()?.Trim() ?? "" : "";

        if (string.IsNullOrEmpty(username))
            return null;

        return (username, password);
    }

    /// <summary>
    /// Quote sheet name for A1 notation (handles numeric names, spaces, special chars)
    /// </summary>
    private static string QuoteSheetName(string sheetName)
    {
        return $"'{sheetName.Replace("'", "''")}'";
    }

    /// <summary>
    /// Convert 0-based column index to Excel-style letter (0=A, 25=Z, 26=AA, etc.)
    /// </summary>
    public static string ColumnIndexToLetter(int index)
    {
        var result = "";
        while (index >= 0)
        {
            result = (char)('A' + index % 26) + result;
            index = index / 26 - 1;
        }
        return result;
    }
}
