using System.IO.Compression;
using BinarySerializer;
using BinarySerializer.Audio.RIFF;
using ImageMagick;
using K4os.Compression.LZ4;

if (args.Length < 2)
{
    Console.WriteLine("Usage: BakesaleExtractor <input_file_or_directory> <output_directory>");
    return;
}

string input = args[0];
string output = args[1];

Directory.CreateDirectory(output);

ISerializerLogger serializerLogger = new FileSerializerLogger(Path.Combine(output, "Log.txt"));
RIFFSettings riffSettings = new();
riffSettings.RegisterChunkResolver("fmt ", (s, data, chunkSize, name) =>
    s.SerializeObject<RIFF_Chunk_ImgFormat>((RIFF_Chunk_ImgFormat)data, x => x.Pre_ChunkSize = chunkSize, name: name));
riffSettings.RegisterChunkResolver("wdta", (s, data, chunkSize, name) =>
    s.SerializeObject<RIFF_Chunk_WaveData>((RIFF_Chunk_WaveData)data, x => x.Pre_ChunkSize = chunkSize, name: name));

if (File.Exists(input))
{
    using Context context = new(Path.GetDirectoryName(input) ?? String.Empty, serializerLogger: serializerLogger);
    context.AddSettings(riffSettings);

    ExtractFile(context, Path.GetFileName(input), output);
}
else if (Directory.Exists(input))
{
    using Context context = new(input, serializerLogger: serializerLogger);
    context.AddSettings(riffSettings);

    foreach (string file in Directory.GetFiles(input, "*.*", SearchOption.AllDirectories))
    {
        if (file.EndsWith(".waves", StringComparison.InvariantCultureIgnoreCase) ||
            file.EndsWith(".sprite", StringComparison.InvariantCultureIgnoreCase))
        {
            ExtractFile(context, Path.GetRelativePath(input, file), output);
        }
    }
}
else
{
    Console.WriteLine($"Input path '{input}' does not exist.");
}

static void ExtractFile(Context context, string fileName, string outputDir)
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