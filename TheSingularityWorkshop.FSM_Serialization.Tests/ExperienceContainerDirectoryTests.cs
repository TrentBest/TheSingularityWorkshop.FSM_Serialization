using System.Buffers.Binary;

namespace TheSingularityWorkshop.FSM_Serialization.Tests;

public sealed class ExperienceContainerDirectoryTests
{
    [Fact]
    public void ReadAcceptsSortedManifestAndIntegrityEntries()
    {
        using var stream = CreateContainer();

        var header = ExperienceContainerHeader.Read(stream);
        var directory = ExperienceContainerDirectory.Read(stream, header);

        Assert.Equal(2, directory.Entries.Count);
        Assert.Equal(ExperienceContainerDirectory.ManifestType, directory.Entries[0].Type);
        Assert.Equal(ExperienceContainerDirectory.IntegrityMetadataType, directory.Entries[1].Type);
    }

    [Fact]
    public void ReadRejectsUnsortedDirectoryEntries()
    {
        using var stream = CreateContainer();
        WriteUInt32(stream, 80, ExperienceContainerDirectory.IntegrityMetadataType);
        WriteUInt32(stream, 160, ExperienceContainerDirectory.ManifestType);

        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsNonzeroSingletonId()
    {
        using var stream = CreateContainer();
        stream.GetBuffer()[88] = 1;

        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsMissingManifest()
    {
        using var stream = CreateContainer();
        WriteUInt32(stream, 80, ExperienceContainerDirectory.ResourceIndexType);

        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsUnknownRequiredSection()
    {
        using var stream = CreateContainerWithUnknownSection(required: true);

        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadAcceptsUnknownOptionalSection()
    {
        using var stream = CreateContainerWithUnknownSection(required: false);

        var directory = ReadDirectory(stream);

        Assert.Equal(3, directory.Entries.Count);
    }

    [Fact]
    public void ReadRejectsReservedSectionFlags()
    {
        using var stream = CreateContainer();
        WriteUInt32(stream, 84, 8);

        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsCompressedIntegrityMetadata()
    {
        using var stream = CreateContainer();
        WriteUInt32(stream, 164, 2);

        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsUnequalLengthsForUncompressedSection()
    {
        using var stream = CreateContainer();
        WriteUInt64(stream, 112, 9);

        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsOverlappingPayloads()
    {
        using var stream = CreateContainer();
        WriteUInt64(stream, 104, 72);

        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsNonzeroPadding()
    {
        using var stream = CreateContainer();
        stream.GetBuffer()[79] = 0x7f;

        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsSectionAboveConfiguredStoredLimit()
    {
        using var stream = CreateContainer();
        WriteUInt64(stream, 104, 64);
        WriteUInt64(stream, 112, 8);

        Assert.Throws<InvalidDataException>(
            () => ExperienceContainerDirectory.Read(stream, ExperienceContainerHeader.Read(stream),
                maximumStoredSectionBytes: 7));
    }

    private static ExperienceContainerDirectory ReadDirectory(Stream stream)
        => ExperienceContainerDirectory.Read(stream, ExperienceContainerHeader.Read(stream));

    private static MemoryStream CreateContainer() => CreateContainerWithUnknownSection(false, includeUnknown: false);

    private static MemoryStream CreateContainerWithUnknownSection(bool required, bool includeUnknown = true)
    {
        int count = includeUnknown ? 3 : 2;
        long directoryOffset = includeUnknown ? 88 : 80;
        long directoryLength = count * 80L;
        long fileLength = directoryOffset + directoryLength;
        var stream = new MemoryStream();
        stream.SetLength(fileLength);

        Span<byte> header = stream.GetBuffer().AsSpan(0, 64);
        "TSWEXP\0\0"u8.CopyTo(header);
        WriteUInt16(stream, 8, 1);
        WriteUInt16(stream, 10, 0);
        WriteUInt32(stream, 12, 64);
        WriteUInt32(stream, 16, 0);
        WriteUInt32(stream, 20, 80);
        WriteUInt32(stream, 24, (uint)count);
        WriteUInt64(stream, 28, (ulong)directoryOffset);
        WriteUInt64(stream, 36, (ulong)directoryLength);
        WriteUInt64(stream, 44, (ulong)fileLength);

        WriteEntry(stream, 64, ExperienceContainerDirectory.ManifestType, 0, 0, 8);
        WriteEntry(stream, 144, ExperienceContainerDirectory.IntegrityMetadataType, 0, 0, 8);
        if (includeUnknown)
            WriteEntry(stream, 224, 99, required ? 1u : 0u, 0, 8);

        stream.Position = 0;
        return stream;
    }

    private static void WriteEntry(MemoryStream stream, int offset, uint type, uint flags, ulong payloadOffset, ulong length)
    {
        WriteUInt32(stream, offset, type);
        WriteUInt32(stream, offset + 4, flags);
        WriteUInt64(stream, offset + 24, payloadOffset);
        WriteUInt64(stream, offset + 32, length);
        WriteUInt64(stream, offset + 40, length);
    }

    private static void WriteUInt16(MemoryStream stream, int offset, ushort value)
        => BinaryPrimitives.WriteUInt16LittleEndian(stream.GetBuffer().AsSpan(offset), value);
    private static void WriteUInt32(MemoryStream stream, int offset, uint value)
        => BinaryPrimitives.WriteUInt32LittleEndian(stream.GetBuffer().AsSpan(offset), value);
    private static void WriteUInt64(MemoryStream stream, int offset, ulong value)
        => BinaryPrimitives.WriteUInt64LittleEndian(stream.GetBuffer().AsSpan(offset), value);
}
