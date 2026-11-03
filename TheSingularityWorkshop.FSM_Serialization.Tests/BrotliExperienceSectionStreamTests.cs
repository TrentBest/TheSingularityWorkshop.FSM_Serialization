using System.IO.Compression;
using System.Security.Cryptography;

namespace TheSingularityWorkshop.FSM_Serialization.Tests;

public sealed class BrotliExperienceSectionStreamTests
{
    [Fact]
    public void ReadStreamsVerifiedBrotliPayloadAndEnforcesDeclaredLength()
    {
        byte[] logical = Enumerable.Range(0, 4096).Select(value => (byte)(value % 251)).ToArray();
        using var source = CreateSource(logical, out var entry);
        var directory = new ExperienceContainerDirectoryAccessor(entry);
        using var decoded = directory.Open(source, entry);

        using var output = new MemoryStream();
        byte[] buffer = new byte[137];
        int read;
        while ((read = decoded.Read(buffer)) > 0)
            output.Write(buffer, 0, read);

        Assert.Equal(logical, output.ToArray());
        Assert.Equal(logical.Length, decoded.Position);
        Assert.Equal(0, decoded.Read(buffer));
    }

    [Fact]
    public void ReadRejectsTruncatedBrotliPayload()
    {
        byte[] logical = Enumerable.Repeat((byte)42, 1024).ToArray();
        using var source = CreateSource(logical, out var entry, truncate: 2);
        var directory = new ExperienceContainerDirectoryAccessor(entry);
        using var decoded = directory.Open(source, entry);

        Assert.Throws<InvalidDataException>(() => Drain(decoded));
    }

    [Fact]
    public void ReadRejectsTrailingStoredBytes()
    {
        byte[] logical = Enumerable.Repeat((byte)7, 128).ToArray();
        using var source = CreateSource(logical, out var entry, appendTrailingByte: true);
        var directory = new ExperienceContainerDirectoryAccessor(entry);
        using var decoded = directory.Open(source, entry);

        Assert.Throws<InvalidDataException>(() => Drain(decoded));
    }

    [Fact]
    public void ReadRejectsLogicalLengthThatIsTooLarge()
    {
        byte[] logical = { 1, 2, 3, 4 };
        using var source = CreateSource(logical, out var entry);
        entry = entry with { LogicalLength = logical.Length - 1 };
        var directory = new ExperienceContainerDirectoryAccessor(entry);
        using var decoded = directory.Open(source, entry);

        Assert.Throws<InvalidDataException>(() => Drain(decoded));
    }

    [Fact]
    public void ReadRejectsLogicalLengthThatIsTooSmall()
    {
        byte[] logical = { 1, 2, 3, 4 };
        using var source = CreateSource(logical, out var entry);
        entry = entry with { LogicalLength = logical.Length + 1 };
        var directory = new ExperienceContainerDirectoryAccessor(entry);
        using var decoded = directory.Open(source, entry);

        Assert.Throws<InvalidDataException>(() => Drain(decoded));
    }

    [Fact]
    public void OpenRejectsLogicalLengthAboveConfiguredLimit()
    {
        byte[] logical = { 1, 2, 3, 4 };
        using var source = CreateSource(logical, out var entry);
        using var verified = VerifiedExperienceSectionStream.Open(source, new object(), entry);

        Assert.Throws<InvalidDataException>(
            () => BrotliExperienceSectionStream.Open(verified, entry.LogicalLength, 3));
    }

    [Fact]
    public void OpenRejectsDigestMismatchBeforeReturningDecoder()
    {
        byte[] logical = { 1, 2, 3, 4 };
        using var source = CreateSource(logical, out var entry);
        source.GetBuffer()[entry.PayloadOffset] ^= 0x40;
        var directory = new ExperienceContainerDirectoryAccessor(entry);

        Assert.Throws<InvalidDataException>(() => directory.Open(source, entry));
    }

    private static void Drain(Stream stream)
    {
        byte[] buffer = new byte[31];
        while (stream.Read(buffer) > 0) { }
    }

    private static MemoryStream CreateSource(
        byte[] logical,
        out ExperienceSectionEntry entry,
        int truncate = 0,
        bool appendTrailingByte = false)
    {
        byte[] compressed = new byte[BrotliEncoder.GetMaxCompressedLength(logical.Length)];
        Assert.True(BrotliEncoder.TryCompress(logical, compressed, out int written, quality: 5, window: 22));
        Array.Resize(ref compressed, written - truncate + (appendTrailingByte ? 1 : 0));
        if (appendTrailingByte)
            compressed[^1] = 0xA5;

        var source = new MemoryStream();
        source.SetLength(64 + compressed.Length);
        compressed.CopyTo(source.GetBuffer(), 64);
        entry = new ExperienceSectionEntry(1, 2, new byte[16], 64, compressed.Length,
            logical.Length, SHA256.HashData(compressed));
        source.Position = 0;
        return source;
    }

    private sealed class ExperienceContainerDirectoryAccessor(ExperienceSectionEntry entry)
    {
        private readonly object _gate = new();
        public BrotliExperienceSectionStream Open(Stream source, ExperienceSectionEntry section)
        {
            using var verified = VerifiedExperienceSectionStream.Open(source, _gate, section);
            // Re-open through the directory-owned gate so the returned decoder owns its stored view.
            return BrotliExperienceSectionStream.Open(
                VerifiedExperienceSectionStream.Open(source, _gate, section), section.LogicalLength, 512L * 1024 * 1024);
        }
    }
}
