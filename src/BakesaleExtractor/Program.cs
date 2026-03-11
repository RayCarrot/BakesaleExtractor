using BinarySerializer;

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

    bool success = extractFile(context, Path.GetFileName(input), output);

    if (!success)
        Console.WriteLine("Unknown file format");
}
else if (Directory.Exists(input))
{
    using Context context = new(input, serializerLogger: serializerLogger);
    context.AddSettings(riffSettings);

    foreach (string file in Directory.GetFiles(input, "*.*", SearchOption.AllDirectories))
        extractFile(context, Path.GetRelativePath(input, file), output);
}
else
{
    Console.WriteLine($"Input path '{input}' does not exist.");
}

static bool extractFile(Context context, string fileName, string output)
{
    if (fileName.EndsWith(".waves", StringComparison.InvariantCultureIgnoreCase) ||
        fileName.EndsWith(".sprite", StringComparison.InvariantCultureIgnoreCase))
    {
        Extractor.ExtractFromRIFFResourceFile(context, fileName, output);
        return true;
    }
    else
    {
        return false;
    }
}