using System.Text.Json;
using KeyMacro.Models;

namespace KeyMacro.Services;

public class DataBundleService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public void Export(string path, DataBundle bundle)
    {
        var json = JsonSerializer.Serialize(bundle, JsonOptions);
        File.WriteAllText(path, json);
        OperationLogger.Info($"DataBundleService.Export: saved to {path}");
    }

    public DataBundle? Import(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var bundle = JsonSerializer.Deserialize<DataBundle>(json, JsonOptions);
            OperationLogger.Info($"DataBundleService.Import: loaded from {path}");
            return bundle;
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"DataBundleService.Import: failed: {ex.Message}");
            return null;
        }
    }
}
