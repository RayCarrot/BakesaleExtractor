using System.Diagnostics.CodeAnalysis;
using System.Text;
using Murmur;

public class StringCache
{
    private Dictionary<uint, string> StringHashes { get; } = new();

    public void Add(string value)
    {
        using Murmur32 hasher = MurmurHash.Create32();
        byte[] hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(value));
        uint hashValue = BitConverter.ToUInt32(hash, 0);
        StringHashes[hashValue] = value;
    }

    public void AddFromFile(string filePath)
    {
        foreach (string value in File.ReadAllLines(filePath))
        {
            if (!String.IsNullOrWhiteSpace(value))
                Add(value);
        }
    }

    public bool TryGetValue(uint hash, [MaybeNullWhen(false)] out string value) => StringHashes.TryGetValue(hash, out value);
}