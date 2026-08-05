using System.IO;
using System.Text.Json;

namespace UbudKusCoin.Services;

public sealed class FinalityStore
{
    private readonly string path;

    public FinalityStore(string path)
    {
        this.path = path;
    }

    public (long Height, string Hash)? Load()
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var record = JsonSerializer.Deserialize<FinalityRecord>(File.ReadAllText(path));
        return record is null ? null : (record.Height, record.Hash);
    }

    public void Save(long height, string hash)
    {
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new FinalityRecord { Height = height, Hash = hash }));
        File.Move(temporary, path, true);
    }

    private sealed class FinalityRecord
    {
        public long Height { get; set; }
        public string Hash { get; set; } = string.Empty;
    }
}
