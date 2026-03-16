using System.IO;
using System.Reflection;
using System.Text;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Exports collections to CSV files via a save file dialog.
/// Ported from ShopManagement.
/// </summary>
public static class CsvExporter
{
    public static bool Export<T>(IEnumerable<T> data, string defaultFileName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = defaultFileName
        };

        if (dialog.ShowDialog() != true)
            return false;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Where(p => !IsCollectionProperty(p))
            .ToArray();

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", properties.Select(p => p.Name)));

        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var val = p.GetValue(item)?.ToString() ?? "";
                return val.Contains(',') || val.Contains('"') || val.Contains('\n') || val.Contains('\r')
                    ? $"\"{val.Replace("\"", "\"\"")}\""
                    : val;
            });
            sb.AppendLine(string.Join(",", values));
        }

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        return true;
    }

    private static bool IsCollectionProperty(PropertyInfo p)
    {
        var type = p.PropertyType;
        if (type == typeof(string)) return false;
        return type.IsAssignableTo(typeof(System.Collections.IEnumerable));
    }
}
