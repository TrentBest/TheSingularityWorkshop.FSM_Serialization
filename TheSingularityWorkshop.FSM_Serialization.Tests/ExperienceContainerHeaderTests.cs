using System.Buffers.Binary;

namespace TheSingularityWorkshop.FSM_Serialization.Tests;

public sealed class ExperienceContainerHeaderTests
{
    [Fact]
    public void ReadAcceptsAValidVersionOneHeader()
    {
        using var stream = CreateContainer();

        var header = ExperienceContainerHeader.Read(stream);

        Assert.Equal((ushort)1, header.MajorVersion);
        Assert.Equal((ushort)0, header.MinorVersion);
        Assert.Equal(2u, header.EntryCount);
        Assert.Equal(64, header.DirectoryOffset);
        Assert.Equal(160, header.DirectoryLength);
        Assert.Equal(224, header.FileLength);
    }

    [Fact]
    public void ReadRejectsNullSource()
        => Assert.Throws<ArgumentNullException>(() => ExperienceContainerHeader.Read(null!));

    [Fact]
    public void ReadRejectsUnreadableSource()
    {
        var stream = new MemoryStream();
        stream.Dispose();

        Assert.Throws<ArgumentException>(() => ExperienceContainerHeader.Read(stream));
    }

    [Fact]
    public void ReadRejectsNonSeekableSource()
    {
        using var stream = new NonSeekableStream(CreateContainer().ToArray());

        Assert.Throws<ArgumentException>(() => ExperienceContainerHeader.Read(stream));
    }

    [Theory]
    [InlineData(63)]
    public void ReadRequiresConfiguredMaximumToFitHeader(long maximum)
    {
        using var stream = CreateContainer();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => ExperienceContainerHeader.Read(stream, maximumContainerBytes: maximum));
    }

    [Fact]
    public void ReadRejectsConfiguredEntryLimitBelowRequiredSingletonCount()
    {
        using var stream = CreateContainer();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => ExperienceContainerHeader.Read(stream, maximumDirectoryEntries: 1));
    }

    [Fact]
    public void ReadRejectsSourceShorterThanHeader()
    {
        using var stream = new MemoryStream(new byte[63]);

        Assert.Throws<InvalidDataException>(() => ExperienceContainerHeader.Read(stream));
    }

    [Fact]
    public void ReadRejectsSourceAboveConfiguredMaximum()
    {
        using var stream = CreateContainer();

        Assert.Throws<InvalidDataException>(
            () => ExperienceContainerHeader.Read(stream, maximumContainerBytes: 223));
    }

    [Fact]
    public void ReadRejectsInvalidMagic()
    {
        using var stream = CreateContainer();
        stream.GetBuffer()[0] = 0;

        Assert.Throws<InvalidDataException>(() => ExperienceContainerHeader.Read(stream));
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(1, 1)]
    public void ReadRejectsUnsupportedFormatVersion(ushort major, ushort minor)
    {
        using var stream = CreateContainer();
        WriteUInt16(stream, 8, major);
        WriteUInt16(stream, 10, minor);

        Assert.Throws<InvalidDataException>(() => ExperienceContainerHeader.Read(stream));
    }

    [Theory]
    [InlineData(63u, 80u)]
    [InlineData(64u, 79u)]
    public void ReadRejectsIncorrectFixedSizes(uint headerSize, uint entrySize)
    {
        using var stream = CreateContainer();
        WriteUInt32(stream, 12, headerSize);
        WriteUInt32(stream, 20, entrySize);

        Assert.Throws<InvalidDataException>(() => ExperienceContainerHeader.Read(stream));
    }

    [Fact]
    public void ReadRejectsNonzeroHeaderFlags()
    {
        using var stream = CreateContainer();
        WriteUInt32(stream, 16, 1);

        Assert.Throws<InvalidDataException>(() => ExperienceContainerHeader.Read(stream));
    }

    [Fact]
    public void ReadRejectsNonzeroReservedHeaderBytes()
    {
        using var stream = CreateContainer();
        stream.GetBuffer()[52] = 1;

        Assert.Throws<InvalidDataException>(() => ExperienceContainerHeader.Read(stream));
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(4097u)]
    public void ReadRejectsEntryCountOutsideConfiguredRange(uint count)
    {
        using var stream = CreateContainer();
        WriteUInt32(stream, 24, count);

        Assert.Throws<InvalidDataException>(
            () => ExperienceContainerHeader.Read(stream, maximumDirectoryEntries: 4096));
    }

    [Fact]
    public void ReadRejectsDirectoryLengthThatDoesNotMatchEntryCount()
    {
        using var stream = CreateContainer();
        WriteUInt64(stream, 36, 159);

        Assert.Throws<InvalidDataException>(() => ExperienceContainerHeader.Read(stream));
    }

    [Theory]
    [InlineData(65ul)]
    [InlineData(56ul)]
    public void ReadRejectsMisalignedOrHeaderOverlappingDirectoryOffset(ulong offset)
    {
        using var stream = CreateContainer();
        WriteUInt64(stream, 28, offset);
        WriteUInt64(stream, 36, 160);
        WriteUInt64(stream, 44, offset + 160);

        Assert.Throws<InvalidDataException>(() => ExperienceContainerHeader.Read(stream));
    }

    [Fact]
    public void ReadRejectsDirectoryOffsetAboveSupportedStreamRange()
    {
        using var stream = CreateContainer();
        WriteUInt64(stream, 28, ulong.MaxValue - 7);
        WriteUInt64(stream, 44, 224);

        Assert.Throws<InvalidDataException>(() => ExperienceContainerHeader.Read(stream));
    }

    [Fact]
    public void ReadRejectsDeclaredLengthDifferentFromActualLength()
    {
        using var stream = CreateContainer();
        WriteUInt64(stream, 44, 225);

        Assert.Throws<InvalidDataException>(() => ExperienceContainerHeader.Read(stream));
    }

    [Fact]
    public void ReadLeavesSourcePositionImmediatelyAfterHeader()
    {
        using var stream = CreateContainer();

        ExperienceContainerHeader.Read(stream);

        Assert.Equal(ExperienceContainerHeader.Size, stream.Position);
    }

    private static MemoryStream CreateContainer()
    {
        var stream = new MemoryStream();
        stream.SetLength(224);
        Span<byte> header = stream.GetBuffer().AsSpan(0, 64);
        "TSWEXP\0\0"u8.CopyTo(header);
        WriteUInt16(stream, 8, 1);
        WriteUInt16(stream, 10, 0);
        WriteUInt32(stream, 12, 64);
        WriteUInt32(stream, 16, 0);
        WriteUInt32(stream, 20, 80);
        WriteUInt32(stream, 24, 2);
        WriteUInt64(stream, 28, 64);
        WriteUInt64(stream, 36, 160);
        WriteUInt64(stream, 44, 224);
        stream.Position = 0;
        return stream;
    }

    private static void WriteUInt16(MemoryStream stream, int offset, ushort value)
        => BinaryPrimitives.WriteUInt16LittleEndian(stream.GetBuffer().AsSpan(offset), value);

    private static void WriteUInt32(MemoryStream stream, int offset, uint value)
        => BinaryPrimitives.WriteUInt32LittleEndian(stream.GetBuffer().AsSpan(offset), value);

    private static void WriteUInt64(MemoryStream stream, int offset, ulong value)
        => BinaryPrimitives.WriteUInt64LittleEndian(stream.GetBuffer().AsSpan(offset), value);

    private sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
    }
}
