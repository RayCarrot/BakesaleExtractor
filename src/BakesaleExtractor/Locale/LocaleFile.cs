#nullable disable
using BinarySerializer;

public class LocaleFile : BinarySerializable
{
    public Pointer Data1Offset { get; set; }
    public int Data1Count { get; set; }
    public Pointer Data2Offset { get; set; }
    public int Data2Count { get; set; }
    public Pointer LanguagesOffset { get; set; }
    public int LanguagesCount { get; set; }

    public int[] Data1 { get; set; }
    public int[] Data2 { get; set; }
    public LocaleLanguage[] Languages { get; set; }

    public override void SerializeImpl(SerializerObject s)
    {
        // Serialize offsets
        Data1Offset = s.SerializePointer(Data1Offset, anchor: s.CurrentPointer, name: nameof(Data1Offset));
        Data1Count = s.Serialize<int>(Data1Count, name: nameof(Data1Count));
        Data2Offset = s.SerializePointer(Data2Offset, anchor: s.CurrentPointer, name: nameof(Data2Offset));
        Data2Count = s.Serialize<int>(Data2Count, name: nameof(Data2Count));
        LanguagesOffset = s.SerializePointer(LanguagesOffset, anchor: s.CurrentPointer, name: nameof(LanguagesOffset));
        LanguagesCount = s.Serialize<int>(LanguagesCount, name: nameof(LanguagesCount));

        // Serialize data from offset
        s.DoAt(Data1Offset, () => Data1 = s.SerializeArray<int>(Data1, Data1Count, name: nameof(Data1)));
        s.DoAt(Data2Offset, () => Data2 = s.SerializeArray<int>(Data2, Data2Count, name: nameof(Data2)));
        s.DoAt(LanguagesOffset, () => Languages = s.SerializeObjectArray<LocaleLanguage>(Languages, LanguagesCount, name: nameof(Languages)));
    }
}