using BinarySerializer;

if (args.Length < 1)
{
    Console.WriteLine("Usage: BakesaleExtractor <input_file_or_directory> [optional_output_directory]");
    return;
}

string input = args[0];

bool inputIsFile = File.Exists(input);
bool inputIsDir = Directory.Exists(input);

if (!inputIsDir && !inputIsFile)
{
    Console.WriteLine($"Input path '{input}' does not exist.");
    return;
}

string output;
if (args.Length > 1)
    output = args[1];
else if (inputIsFile)
    output = Path.GetDirectoryName(input) ?? String.Empty;
else
    output = input;

Directory.CreateDirectory(output);

ISerializerLogger serializerLogger = new FileSerializerLogger(Path.Combine(output, "Log.txt"));
RIFFSettings riffSettings = new();
riffSettings.RegisterChunkResolver("sprs", (s, data, chunkSize, name) =>
    s.SerializeObject<RIFF_Chunk_Sprites>((RIFF_Chunk_Sprites)data, x => x.Pre_ChunkSize = chunkSize, name: name));
riffSettings.RegisterChunkResolver("fmt ", (s, data, chunkSize, name) =>
    s.SerializeObject<RIFF_Chunk_ImgFormat>((RIFF_Chunk_ImgFormat)data, x => x.Pre_ChunkSize = chunkSize, name: name));
riffSettings.RegisterChunkResolver("wdta", (s, data, chunkSize, name) =>
    s.SerializeObject<RIFF_Chunk_WaveData>((RIFF_Chunk_WaveData)data, x => x.Pre_ChunkSize = chunkSize, name: name));

StringCache stringCache = new();
if (Directory.Exists("Strings"))
{
    foreach (string stringsFilePath in Directory.GetFiles("Strings", "*.txt"))
        stringCache.AddFromFile(stringsFilePath);
}

if (inputIsFile)
{
    using Context context = new(Path.GetDirectoryName(input) ?? String.Empty, serializerLogger: serializerLogger);
    context.AddSettings(riffSettings);

    bool success = extractFile(context, stringCache, Path.GetFileName(input), output);

    if (!success)
        Console.WriteLine("Unknown file format");
}
else if (inputIsDir)
{
    using Context context = new(input, serializerLogger: serializerLogger);
    context.AddSettings(riffSettings);

    foreach (string file in Directory.GetFiles(input, "*.*", SearchOption.AllDirectories))
        extractFile(context, stringCache, Path.GetRelativePath(input, file), output);
}

static bool extractFile(Context context, StringCache stringCache, string fileName, string output)
{
    if (fileName.EndsWith(".waves", StringComparison.InvariantCultureIgnoreCase) ||
        fileName.EndsWith(".sprite", StringComparison.InvariantCultureIgnoreCase))
    {
        Extractor.ExtractFromRIFFResourceFile(context, stringCache, fileName, output);
        return true;
    }
    else if (fileName.EndsWith(".strings", StringComparison.InvariantCultureIgnoreCase))
    {
        Extractor.ExtractFromLocaleFile(context, stringCache, fileName, output);
        return true;
    }
    else
    {
        return false;
    }
}