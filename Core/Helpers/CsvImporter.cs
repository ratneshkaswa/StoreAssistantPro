using System.IO;
using System.Text;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Imports CSV files via an open file dialog and parses rows as dictionaries.
/// Ported from ShopManagement.
/// </summary>
public static class CsvImporter
{
    /// <summary>
    /// Reads a CSV file chosen by the user and returns parsed rows as dictionaries.
    /// Keys are column headers (trimmed), values are cell strings.
    /// Returns null if the user cancels the dialog.
    /// </summary>
    public static List<Dictionary<string, string>>? Import()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return null;

        var lines = File.ReadAllLines(dialog.FileName);
        if (lines.Length < 2)
            return [];

        var headers = ParseCsvLine(lines[0]);
        var rows = new List<Dictionary<string, string>>();

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var values = ParseCsvLine(line);
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int j = 0; j < headers.Count; j++)
                row[headers[j]] = j < values.Count ? values[j] : "";

            rows.Add(row);
        }

        return rows;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        int i = 0;

        while (i < line.Length)
        {
            if (line[i] == '"')
            {
                i++;
                var field = new StringBuilder();
                while (i < line.Length)
                {
                    if (line[i] == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            field.Append('"');
                            i += 2;
                        }
                        else
                        {
                            i++;
                            break;
                        }
                    }
                    else
                    {
                        field.Append(line[i]);
                        i++;
                    }
                }
                fields.Add(field.ToString().Trim());
                if (i < line.Length && line[i] == ',') i++;
            }
            else
            {
                int start = i;
                while (i < line.Length && line[i] != ',') i++;
                fields.Add(line[start..i].Trim());
                if (i < line.Length) i++;
            }
        }

        return fields;
    }
}
