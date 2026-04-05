using System.IO.Compression;
using BinarySerializer;
using BinarySerializer.Audio.RIFF;
using ImageMagick;
using K4os.Compression.LZ4;

public static class Extractor
{
    private static string GetExportPath(string outputDir, string filePath)
    {
        string dir = Path.GetDirectoryName(filePath) ?? String.Empty;
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        return Path.Combine(outputDir, dir, fileName);
    }

    private static void EnsureFileDirectoryExists(string filePath)
    {
        if (Path.GetDirectoryName(filePath) is { } outputPathDir)
            Directory.CreateDirectory(outputPathDir);
    }

    public static void ExtractFromRIFFResourceFile(Context context, StringCache stringCache, string fileName, string outputDir)
    {
        Console.WriteLine($"Extracting from {fileName}");

        context.AddFile(new LinearFile(context, fileName));
        RIFF_Chunk riff = FileFactory.Read<RIFF_Chunk>(context, fileName);

        // Sprites
        if (riff.Data is RIFF_Chunk_RIFF { Type: "SPRT" } sprites)
        {
            string spritesOutputDir = GetExportPath(outputDir, fileName);

            RIFF_Chunk_List? list = sprites.GetChunk<RIFF_Chunk_List>();

            if (list == null)
            {
                Console.WriteLine("WARNING: Sprites file is missing sprite list. Skipping file.");
                return;
            }

            List<MagickImage> images = [];
            try
            {
                foreach (RIFF_Chunk chunk in list.Chunks)
                {
                    if (chunk.Data is RIFF_Chunk_RIFF { Type: "IMG " } img)
                    {
                        RIFF_Chunk_ImgFormat? fmt = img.GetChunk<RIFF_Chunk_ImgFormat>();
                        RIFF_Chunk_Data? data = img.GetChunk<RIFF_Chunk_Data>();

                        if (fmt == null || data == null)
                        {
                            Console.WriteLine("WARNING: Sprite is missing format or data chunk. Skipping sprite.");
                            continue;
                        }

                        Console.WriteLine($"Extracting {fmt.Name}");

                        byte[] imgData = new byte[fmt.Width * fmt.Height * 4];
                        LZ4Codec.Decode(data.Data, imgData);

                        string outputPath = Path.Combine(spritesOutputDir, "_sheets", $"{fmt.Name}.png");
                        EnsureFileDirectoryExists(outputPath);

                        MagickImage image = new(imgData, new MagickReadSettings()
                        {
                            Width = fmt.Width,
                            Height = fmt.Height,
                            Format = MagickFormat.Rgba,
                        });
                        image.Write(outputPath);
                        images.Add(image);
                    }
                }

                RIFF_Chunk_Sprites? sprs = sprites.GetChunk<RIFF_Chunk_Sprites>();

                if (sprs != null)
                {
                    Dictionary<int, uint> spriteHashes = new();
                    for (int i = 0; i < sprs.SpriteNameHashes.Length; i++)
                    {
                        uint hash = sprs.SpriteNameHashes[i];
                        if (hash != 0)
                            spriteHashes.Add(sprs.NameHashIndexToSpriteIndexTable[i], hash);
                    }

                    Console.WriteLine("Extracting sprites");

                    for (int i = 0; i < sprs.Sprites.Length; i++)
                    {
                        Sprite sprite = sprs.Sprites[i];
                        using IMagickImage<byte> image = images[sprite.ImageIndex].Clone();
                        image.Crop(new MagickGeometry(sprite.XPosition, sprite.YPosition, (uint)sprite.Width, (uint)sprite.Height));

                        string spriteOutputPath;
                        uint hash = spriteHashes[i];
                        if (stringCache.TryGetValue(hash, out string? name))
                            spriteOutputPath = Path.Combine(spritesOutputDir, $"{name}.png");
                        else
                            spriteOutputPath = Path.Combine(spritesOutputDir, "_unnamed", $"{i}_{hash:X8}.png");

                        EnsureFileDirectoryExists(spriteOutputPath);

                        image.Write(spriteOutputPath);
                    }
                }
            }
            finally
            {
                foreach (MagickImage img in images)
                    img.Dispose();
            }
        }
        // Waves
        else if (riff.Data is RIFF_Chunk_RIFF { Type: "WBNK" } waveBank)
        {
            RIFF_Chunk_List? list = waveBank.GetChunk<RIFF_Chunk_List>();

            if (list == null)
            {
                Console.WriteLine("WARNING: Waves file is missing wave list. Skipping file.");
                return;
            }

            int index = 0;
            foreach (RIFF_Chunk chunk in list.Chunks)
            {
                if (chunk.Data is RIFF_Chunk_WaveData waveData)
                {
                    using ZLibStream zlibStream = new(new MemoryStream(waveData.Data), CompressionMode.Decompress);

                    string outputPath = $"{GetExportPath(outputDir, fileName)}_{index:00}.wav";
                    if (Path.GetDirectoryName(outputPath) is { } outputPathDir)
                        Directory.CreateDirectory(outputPathDir);

                    using Stream fileStream = File.Create(outputPath);
                    zlibStream.CopyTo(fileStream);
                }
                else
                {
                    Console.WriteLine("WARNING: Unknown wave data. Skipping wave.");
                }

                index++;
            }
        }
        else
        {
            Console.WriteLine("WARNING: Unknown format. Skipping file.");
        }

    }

    public static void ExtractFromLocaleFile(Context context, string fileName, string outputDir)
    {
        Console.WriteLine($"Extracting from {fileName}");

        context.AddFile(new LinearFile(context, fileName));
        LocaleFile locale = FileFactory.Read<LocaleFile>(context, fileName);

        foreach (LocaleLanguage language in locale.Languages)
        {
            string outputPath = $"{GetExportPath(outputDir, fileName)}_{language.LanguageCode}.txt";
            EnsureFileDirectoryExists(outputPath);

            File.WriteAllLines(outputPath, language.Strings.Select(x => x.Value));
        }
    }
}