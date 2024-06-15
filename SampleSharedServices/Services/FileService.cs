using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;

namespace SampleSharedServices.Services;

public static class FileService
{
    public static string GetCSVString<T>(List<T> logs)
    {
        using var document = JsonSerializer.SerializeToDocument(logs);
        JsonElement.ArrayEnumerator root = document.RootElement.EnumerateArray();
        var builder = new StringBuilder();
        var headers = root.First().EnumerateObject().Select(o => o.Name);
        builder.AppendJoin(',', headers);
        builder.AppendLine();
        foreach (var row in root)
        {
            var values = row.EnumerateObject()
                .Select(o =>
                    o.Value.ToString().Replace("\n", "").Replace("\r", "").Replace(",", "")
                );
            builder.AppendJoin(',', values);
            builder.AppendLine();
        }
        return builder.ToString();
    }

    public static List<T> GetCSVFile<T>(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<T>();
        return records.ToList();
    }

    public static void SaveFile(string filePath, byte[] data)
    {
        File.WriteAllBytes(filePath, data);
    }
}
