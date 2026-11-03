using System.Buffers.Binary;
using System.Security.Cryptography;

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
    public void OpenVerifiedSectionReturnsBoundedReadOnlyView()
    {
        using var stream = CreateContainer();
        var directory = ReadDirectory(stream);
        using var section = directory.OpenVerifiedSection(stream, directory.Entries[0]);

        Assert.Equal(6, section.Length);
        Assert.Equal(6, section.Read(new byte[10], 0, 10));
        Assert.Equal(0, section.Read(new byte[1], 0, 1));
        Assert.Throws<NotSupportedException>(() => section.Write(new byte[1], 0, 1));
    }

    [Fact]
    public void OpenVerifiedSectionRejectsDigestMismatchBeforeReturningView()
    {
        using var stream = CreateContainer();
        var directory = ReadDirectory(stream);
        stream.GetBuffer()[64] = 0x42;

        Assert.Throws<InvalidDataException>(
            () => directory.OpenVerifiedSection(stream, directory.Entries[0]));
    }

    [Fact]
    public void OpenVerifiedSectionRejectsEntryFromAnotherDirectory()
    {
        using var first = CreateContainer();
        using var second = CreateContainer();
        var firstDirectory = ReadDirectory(first);
        var secondDirectory = ReadDirectory(second);

        Assert.Throws<ArgumentException>(
            () => firstDirectory.OpenVerifiedSection(first, secondDirectory.Entries[0]));
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
        WriteUInt64(stream, 184, 68);

        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsNonzeroPadding()
    {
        using var stream = CreateContainer();
        stream.GetBuffer()[70] = 0x7f;

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


    [Fact]
    public void ReadRejectsNullSource()
        => Assert.Throws<ArgumentNullException>(() => ExperienceContainerDirectory.Read(null!, default));

    [Fact]
    public void ReadRejectsNegativeConfiguredLimits()
    {
        using var stream = CreateContainer();
        var header = ExperienceContainerHeader.Read(stream);
        Assert.Throws<ArgumentOutOfRangeException>(() => ExperienceContainerDirectory.Read(stream, header, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExperienceContainerDirectory.Read(stream, header, maximumLogicalSectionBytes: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExperienceContainerDirectory.Read(stream, header, maximumAggregateLogicalBytes: -1));
    }

    [Fact]
    public void ReadRejectsSectionAboveConfiguredLogicalLimit()
    {
        using var stream = CreateContainer();
        Assert.Throws<InvalidDataException>(() => ExperienceContainerDirectory.Read(
            stream, ExperienceContainerHeader.Read(stream), maximumLogicalSectionBytes: 5));
    }

    [Fact]
    public void ReadRejectsAggregateLogicalLimit()
    {
        using var stream = CreateContainer();
        Assert.Throws<InvalidDataException>(() => ExperienceContainerDirectory.Read(
            stream, ExperienceContainerHeader.Read(stream), maximumAggregateLogicalBytes: 13));
    }

    [Fact]
    public void ReadRejectsSectionOffsetAboveSupportedRange()
    {
        using var stream = CreateContainer();
        WriteUInt64(stream, 104, ulong.MaxValue);
        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsSectionStoredLengthAboveSupportedRange()
    {
        using var stream = CreateContainer();
        WriteUInt64(stream, 112, ulong.MaxValue);
        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsSectionLogicalLengthAboveSupportedRange()
    {
        using var stream = CreateContainer();
        WriteUInt64(stream, 120, ulong.MaxValue);
        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsUnsupportedCompressionValue()
    {
        using var stream = CreateContainer();
        WriteUInt32(stream, 84, 4);
        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsMisalignedPayloadOffset()
    {
        using var stream = CreateContainer();
        WriteUInt64(stream, 104, 65);
        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsPayloadOverlappingHeader()
    {
        using var stream = CreateContainer();
        WriteUInt64(stream, 104, 56);
        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsPayloadOverlappingDirectory()
    {
        using var stream = CreateContainer();
        WriteUInt64(stream, 104, 80);
        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsMissingIntegrityMetadata()
    {
        using var stream = CreateContainer();
        WriteUInt32(stream, 160, 99);
        WriteUInt32(stream, 164, 0);
        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsDuplicateSingletonManifest()
    {
        using var stream = CreateContainer();
        WriteUInt32(stream, 160, ExperienceContainerDirectory.ManifestType);
        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsDuplicateResourceIndex()
    {
        using var stream = CreateContainerWithUnknownSection(required: false);
        WriteUInt32(stream, 96, ExperienceContainerDirectory.ResourceIndexType);
        WriteUInt32(stream, 176, ExperienceContainerDirectory.ResourceIndexType);
        WriteUInt32(stream, 256, ExperienceContainerDirectory.IntegrityMetadataType);
        // Keep canonical ordering and make the third entry's payload the integrity record.
        WriteUInt32(stream, 96 + 4, 0);
        WriteUInt32(stream, 176 + 4, 0);
        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsZeroIdForRepeatedSectionType()
    {
        using var stream = CreateContainerWithUnknownSection(required: false);
        WriteUInt32(stream, 256, ExperienceContainerDirectory.OverridesType);
        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadRejectsDuplicateRepeatedSectionId()
    {
        using var stream = CreateContainerWithUnknownSection(required: false);
        WriteUInt32(stream, 96, ExperienceContainerDirectory.OverridesType);
        WriteUInt32(stream, 176, ExperienceContainerDirectory.OverridesType);
        // Both IDs are zero, which is rejected before duplicate-ID detection.
        Assert.Throws<InvalidDataException>(() => ReadDirectory(stream));
    }

    [Fact]
    public void ReadAcceptsCompressedNonIntegritySectionWithDifferentStoredAndLogicalLengths()
    {
        using var stream = CreateContainerWithUnknownSection(required: false);
        // Unknown optional sections may declare Brotli data and a different logical size.
        WriteUInt32(stream, 260, 2);
        WriteUInt64(stream, 288, 4);
        Assert.Equal(3, ReadDirectory(stream).Entries.Count);
    }

    [Fact]
    public void OpenVerifiedSectionRejectsUnreadableSource()
    {
        using var stream = CreateContainer();
        var directory = ReadDirectory(stream);
        stream.Dispose();
        Assert.Throws<ArgumentException>(() => directory.OpenVerifiedSection(stream, directory.Entries[0]));
    }

    [Fact]
    public void OpenVerifiedSectionRejectsNullEntry()
    {
        using var stream = CreateContainer();
        var directory = ReadDirectory(stream);
        Assert.Throws<ArgumentNullException>(() => directory.OpenVerifiedSection(stream, null!));
    }

    private static ExperienceContainerDirectory ReadDirectory(Stream stream)
        => ExperienceContainerDirectory.Read(stream, ExperienceContainerHeader.Read(stream));

    private static MemoryStream CreateContainer() => CreateContainerWithUnknownSection(false, includeUnknown: false);

    private static MemoryStream CreateContainerWithUnknownSection(bool required, bool includeUnknown = true)
    {
        int count = includeUnknown ? 3 : 2;
        long directoryOffset = includeUnknown ? 96 : 80;
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

        WriteEntry(stream, includeUnknown ? 96 : 80, ExperienceContainerDirectory.ManifestType, 0, 64, 6);
        WriteEntry(stream, includeUnknown ? 176 : 160, ExperienceContainerDirectory.IntegrityMetadataType, 0, 72, 8);
        WriteDigest(stream, includeUnknown ? 96 : 80, new byte[6]);
        WriteDigest(stream, includeUnknown ? 176 : 160, new byte[8]);
        if (includeUnknown)
            WriteEntry(stream, 256, 99, required ? 1u : 0u, 80, 8);

        stream.Position = 0;
        return stream;
    }

    private static void WriteDigest(MemoryStream stream, int entryOffset, byte[] payload)
        => SHA256.HashData(payload).CopyTo(stream.GetBuffer(), entryOffset + 48);

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
