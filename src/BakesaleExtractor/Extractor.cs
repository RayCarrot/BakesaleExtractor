using System.IO.Compression;
using BinarySerializer;
using BinarySerializer.Audio.RIFF;
using ImageMagick;
using K4os.Compression.LZ4;

public static class Extractor
{
    public static void ExtractFromRIFFResourceFile(Context context, string fileName, string outputDir)
    {
        Console.WriteLine($"Extracting from {fileName}");

        context.AddFile(new LinearFile(context, fileName));
        RIFF_Chunk riff = FileFactory.Read<RIFF_Chunk>(context, fileName);

        // Sprites
        if (riff.Data is RIFF_Chunk_RIFF { Type: "SPRT" } sprites)
        {
            RIFF_Chunk_List? list = sprites.GetChunk<RIFF_Chunk_List>();

            if (list == null)
            {
                Console.WriteLine("WARNING: Sprites file is missing sprite list. Skipping file.");
                return;
            }

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

                    string outputPath = Path.Combine(outputDir, $"{fileName}_{fmt.Name}.png");
                    if (Path.GetDirectoryName(outputPath) is { } outputPathDir)
                        Directory.CreateDirectory(outputPathDir);

                    MagickImage image = new(imgData, new MagickReadSettings()
                    {
                        Width = fmt.Width,
                        Height = fmt.Height,
                        Format = MagickFormat.Rgba,
                    });
                    image.Write(outputPath);
                }
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

                    string outputPath = Path.Combine(outputDir, $"{fileName}_{index:00}.wav");
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
            string outputPath = Path.Combine(outputDir, $"{fileName}_{language.LanguageCode}.txt");
            if (Path.GetDirectoryName(outputPath) is { } outputPathDir)
                Directory.CreateDirectory(outputPathDir);

            File.WriteAllLines(outputPath, language.Strings.Select(x => x.Value));
        }
    }
}