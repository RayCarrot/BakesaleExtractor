# Bakesale (Eclipse Engine) Extractor
A very basic extractor for the file formats used in the Bakesale/Eclipse engine. This has only been tested on the Rayman 30th Anniversary Edition, so compatibility with other games might vary!

## Supported formats
- **Audio (.waves)** -> .wav files
- **Sprites (.sprite)** -> .png files
- **Text (.strings)** -> .txt files

## Usage
This app is a command-line app meaning it has no interface. You can run it by open the command-prompt and run the following command (replacing the arguments with your paths):
```
BakesaleExtractor <input_file_or_directory> [optional_output_directory]
```
Alternatively you can drag-drop a file or folder onto the exe and it'll extract them to the same directory. A `Log.txt` file will also be generated upon extracting, showing the serialized file data.
