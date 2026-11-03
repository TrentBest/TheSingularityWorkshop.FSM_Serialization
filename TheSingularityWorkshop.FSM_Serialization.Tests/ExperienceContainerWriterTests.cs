using System.Buffers.Binary;

namespace TheSingularityWorkshop.FSM_Serialization.Tests;

public sealed class ExperienceContainerWriterTests
{
    [Fact]
    public void WriteProducesDeterministicGoldenVersionOneLayout()
    {
        byte[] first = WriteGoldenContainer();
        byte[] second = WriteGoldenContainer();

        Assert.Equal(first, second);
        Assert.Equal(328, first.Length);
        Assert.Equal("TSWEXP\0\0"u8.ToArray(), first[..8]);
        Assert.Equal((ushort)1, BinaryPrimitives.ReadUInt16LittleEndian(first.AsSpan(8)));
        Assert.Equal((ushort)0, BinaryPrimitives.ReadUInt16LittleEndian(first.AsSpan(10)));
        Assert.Equal(88UL, BinaryPrimitives.ReadUInt64LittleEndian(first.AsSpan(28)));
        Assert.Equal(240UL, BinaryPrimitives.ReadUInt64LittleEndian(first.AsSpan(36)));
        Assert.Equal(328UL, BinaryPrimitives.ReadUInt64LittleEndian(first.AsSpan(44)));

        // Physical payload order: manifest, integrity metadata, overrides.
        Assert.Equal(new byte[] { 0x4D, 0x46 }, first[64..66]);
        Assert.Equal(new byte[] { 0x49, 0x4E, 0x54 }, first[72..75]);
        Assert.Equal(new byte[] { 0x4F }, first[80..81]);
        Assert.All(first[66..72], value => Assert.Equal((byte)0, value));
        Assert.All(first[75..80], value => Assert.Equal((byte)0, value));
        Assert.All(first[81..88], value => Assert.Equal((byte)0, value));

        // Canonical directory order: manifest, overrides, integrity metadata.
        Assert.Equal(ExperienceContainerDirectory.ManifestType,
            BinaryPrimitives.ReadUInt32LittleEndian(first.AsSpan(88)));
        Assert.Equal(64UL, BinaryPrimitives.ReadUInt64LittleEndian(first.AsSpan(112)));
        Assert.Equal("9F2A9C895DF3495AA6CDF92531BB22665E17E2BCA64A05469725A0F9D873CAD9",
            Convert.ToHexString(first.AsSpan(136, 32)));

        Assert.Equal(ExperienceContainerDirectory.OverridesType,
            BinaryPrimitives.ReadUInt32LittleEndian(first.AsSpan(168)));
        Assert.Equal(80UL, BinaryPrimitives.ReadUInt64LittleEndian(first.AsSpan(192)));
        Assert.Equal("C4694F2E93D5C4E7D51F9C5DEB75E6CC8BE5E1114178C6A45B6FC2C566A0AA8C",
            Convert.ToHexString(first.AsSpan(216, 32)));

        Assert.Equal(ExperienceContainerDirectory.IntegrityMetadataType,
            BinaryPrimitives.ReadUInt32LittleEndian(first.AsSpan(248)));
        Assert.Equal(72UL, BinaryPrimitives.ReadUInt64LittleEndian(first.AsSpan(272)));
        Assert.Equal("370FBB51F069DDC08E23504F7F3A8D85D629664229F8B77F85EED5B8F5A267ED",
            Convert.ToHexString(first.AsSpan(296, 32)));
    }

    [Fact]
    public void WriteOutputCanBeReadAndEverySectionVerified()
    {
        using var output = new MemoryStream(WriteGoldenContainer(), writable: false);

        ExperienceContainerHeader header = ExperienceContainerHeader.Read(output);
        ExperienceContainerDirectory directory = ExperienceContainerDirectory.Read(output, header);

        Assert.Equal(3, directory.Entries.Count);
        foreach (ExperienceSectionEntry entry in directory.Entries)
        {
            using VerifiedExperienceSectionStream section = directory.OpenVerifiedSection(output, entry);
            Assert.Equal(entry.StoredLength, section.Length);
            Assert.Equal(entry.StoredLength, Drain(section));
        }
    }

    [Fact]
    public void WriteRejectsSourceThatChangesBetweenHashAndEmissionPasses()
    {
        int opens = 0;
        var changing = new ExperienceSectionSource(
            ExperienceContainerDirectory.ManifestType, new byte[16], true, 1,
            () => new MemoryStream(new[] { (byte)(++opens == 1 ? 1 : 2) }, writable: false));
        var integrity = Source(ExperienceContainerDirectory.IntegrityMetadataType, new byte[16], new byte[] { 9 });

        using var destination = new MemoryStream();
        Assert.Throws<InvalidDataException>(() =>
            ExperienceContainerWriter.Write(destination, new[] { changing, integrity }));
    }

    [Fact]
    public void WriteRejectsMissingSingletonSections()
    {
        var onlyManifest = Source(ExperienceContainerDirectory.ManifestType, new byte[16], new byte[] { 1 });
        Assert.Throws<InvalidDataException>(() =>
            ExperienceContainerWriter.Write(new MemoryStream(), new[] { onlyManifest }));
    }

    [Fact]
    public void WriteRejectsZeroIdForRepeatedSection()
    {
        var sections = new[]
        {
            Source(ExperienceContainerDirectory.ManifestType, new byte[16], new byte[] { 1 }),
            Source(ExperienceContainerDirectory.OverridesType, new byte[16], new byte[] { 2 }),
            Source(ExperienceContainerDirectory.IntegrityMetadataType, new byte[16], new byte[] { 3 })
        };
        Assert.Throws<InvalidDataException>(() => ExperienceContainerWriter.Write(new MemoryStream(), sections));
    }

    [Fact]
    public void WriteRejectsSeekableDestinationNotAtBeginning()
    {
        using var destination = new MemoryStream();
        destination.WriteByte(0x7F);
        var sections = GoldenSections();
        Assert.Throws<ArgumentException>(() => ExperienceContainerWriter.Write(destination, sections));
    }

    [Fact]
    public void WriteRejectsSectionAboveConfiguredLimit()
    {
        Assert.Throws<InvalidDataException>(() => ExperienceContainerWriter.Write(
            new MemoryStream(), GoldenSections(), maximumSectionBytes: 2));
    }

    [Fact]
    public void WriteSupportsNonSeekableDestination()
    {
        using var storage = new MemoryStream();
        using (var destination = new NonSeekableWriteStream(storage))
            ExperienceContainerWriter.Write(destination, GoldenSections());

        storage.Position = 0;
        ExperienceContainerHeader header = ExperienceContainerHeader.Read(storage);
        Assert.Equal(3, ExperienceContainerDirectory.Read(storage, header).Entries.Count);
    }

    [Fact]
    public void WriteRejectsDuplicateOptionalDirectoryKeys()
    {
        byte[] optionalId = new byte[16];
        optionalId[0] = 7;
        var sections = new[]
        {
            Source(ExperienceContainerDirectory.ManifestType, new byte[16], new byte[] { 1 }),
            Source(99, optionalId, new byte[] { 2 }) with { Required = false },
            Source(99, optionalId, new byte[] { 3 }) with { Required = false },
            Source(ExperienceContainerDirectory.IntegrityMetadataType, new byte[16], new byte[] { 4 })
        };
        Assert.Throws<InvalidDataException>(() => ExperienceContainerWriter.Write(new MemoryStream(), sections));
    }

    [Fact]
    public void WriteRejectsUnknownRequiredSection()
    {
        var sections = GoldenSections().Append(
            Source(99, new byte[16], new byte[] { 5 })).ToArray();
        Assert.Throws<InvalidDataException>(() => ExperienceContainerWriter.Write(new MemoryStream(), sections));
    }

    [Fact]
    public void WriteRejectsAggregateAndContainerLimits()
    {
        Assert.Throws<InvalidDataException>(() => ExperienceContainerWriter.Write(
            new MemoryStream(), GoldenSections(), maximumAggregateLogicalBytes: 5));
        Assert.Throws<InvalidDataException>(() => ExperienceContainerWriter.Write(
            new MemoryStream(), GoldenSections(), maximumContainerBytes: 100));
    }

    [Fact]
    public void WriteRejectsSourceLongerThanDeclaredLength()
    {
        var source = new ExperienceSectionSource(
            ExperienceContainerDirectory.ManifestType, new byte[16], true, 1,
            () => new MemoryStream(new byte[] { 1, 2 }, writable: false));
        var integrity = Source(ExperienceContainerDirectory.IntegrityMetadataType, new byte[16], new byte[] { 3 });
        Assert.Throws<InvalidDataException>(() =>
            ExperienceContainerWriter.Write(new MemoryStream(), new[] { source, integrity }));
    }

    [Fact]
    public void WriteRejectsSourceShorterThanDeclaredLength()
    {
        var source = new ExperienceSectionSource(
            ExperienceContainerDirectory.ManifestType, new byte[16], true, 2,
            () => new MemoryStream(new byte[] { 1 }, writable: false));
        var integrity = Source(ExperienceContainerDirectory.IntegrityMetadataType, new byte[16], new byte[] { 3 });
        Assert.Throws<EndOfStreamException>(() =>
            ExperienceContainerWriter.Write(new MemoryStream(), new[] { source, integrity }));
    }

    private sealed class NonSeekableWriteStream(Stream inner) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => inner.Flush();
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => inner.Write(buffer);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                inner.Flush();
            base.Dispose(disposing);
        }
    }

    private static byte[] WriteGoldenContainer()
    {
        using var output = new MemoryStream();
        ExperienceContainerWriter.Write(output, GoldenSections());
        return output.ToArray();
    }

    private static ExperienceSectionSource[] GoldenSections()
    {
        byte[] zero = new byte[16];
        byte[] overrideId = new byte[16];
        overrideId[0] = 1;
        return new[]
        {
            Source(ExperienceContainerDirectory.OverridesType, overrideId, new byte[] { 0x4F }),
            Source(ExperienceContainerDirectory.IntegrityMetadataType, zero, new byte[] { 0x49, 0x4E, 0x54 }),
            Source(ExperienceContainerDirectory.ManifestType, zero, new byte[] { 0x4D, 0x46 })
        };
    }

    private static ExperienceSectionSource Source(uint type, byte[] id, byte[] payload)
        => new(type, id, true, payload.Length, () => new MemoryStream(payload, writable: false));

    private static long Drain(Stream stream)
    {
        byte[] buffer = new byte[17];
        long total = 0;
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
            total += read;
        return total;
    }
}
