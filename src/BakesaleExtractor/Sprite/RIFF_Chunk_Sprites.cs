#nullable disable
using BinarySerializer;
using BinarySerializer.Audio.RIFF;

public class RIFF_Chunk_Sprites : RIFF_ChunkData
{
    public override string ChunkIdentifier => "sprs";

    public int ImagesCount { get; set; }
    public int TablesLength { get; set; }
    public int SpritesCount { get; set; }

    public int[] SpriteNameHashes { get; set; } // Seems to use MurmurHash
    public int[] NameHashIndexToSpriteIndexTable { get; set; }
    public Sprite[] Sprites { get; set; }

    public override void SerializeImpl(SerializerObject s)
    {
        ImagesCount = s.Serialize<int>(ImagesCount, name: nameof(ImagesCount));
        TablesLength = s.Serialize<int>(TablesLength, name: nameof(TablesLength));
        SpritesCount = s.Serialize<int>(SpritesCount, name: nameof(SpritesCount));
        SpriteNameHashes = s.SerializeArray<int>(SpriteNameHashes, TablesLength, name: nameof(SpriteNameHashes));
        NameHashIndexToSpriteIndexTable = s.SerializeArray<int>(NameHashIndexToSpriteIndexTable, TablesLength, name: nameof(NameHashIndexToSpriteIndexTable));
        Sprites = s.SerializeObjectArray<Sprite>(Sprites, SpritesCount, name: nameof(Sprites));
    }
}