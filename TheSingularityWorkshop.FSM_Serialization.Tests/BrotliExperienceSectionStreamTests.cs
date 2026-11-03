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


    [Fact]
    public void ReadRejectsMalformedBrotliPayload()
    {
        byte[] malformed = Enumerable.Repeat((byte)0xFF, 32).ToArray();
        using var source = CreateRawSource(malformed, out var entry, logicalLength: 16);
        var directory = new ExperienceContainerDirectoryAccessor(entry);

        using var decoded = directory.Open(source, entry);
        Assert.Throws<InvalidDataException>(() => Drain(decoded));
    }

    [Fact]
    public void EmptyLogicalPayloadCompletesWithoutOutput()
    {
        byte[] logical = Array.Empty<byte>();
        using var source = CreateSource(logical, out var entry);
        var directory = new ExperienceContainerDirectoryAccessor(entry);
        using var decoded = directory.Open(source, entry);

        Assert.Equal(0, decoded.Read(new byte[1]));
        Assert.Equal(0, decoded.Position);
    }

    [Fact]
    public void ReadValidatesArgumentsAndDisposedState()
    {
        byte[] logical = { 1, 2, 3 };
        using var source = CreateSource(logical, out var entry);
        var directory = new ExperienceContainerDirectoryAccessor(entry);
        var decoded = directory.Open(source, entry);

        Assert.True(decoded.CanRead);
        Assert.False(decoded.CanSeek);
        Assert.False(decoded.CanWrite);
        Assert.Throws<ArgumentNullException>(() => decoded.Read(null!, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => decoded.Read(new byte[2], -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => decoded.Read(new byte[2], 1, 2));
        Assert.Throws<NotSupportedException>(() => decoded.Position = 1);
        Assert.Throws<NotSupportedException>(() => decoded.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => decoded.SetLength(0));
        Assert.Throws<NotSupportedException>(() => decoded.Write(new byte[1], 0, 1));
        decoded.Dispose();
        Assert.False(decoded.CanRead);
        Assert.Throws<ObjectDisposedException>(() => decoded.Read(new byte[1]));
        Assert.Throws<ObjectDisposedException>(() => _ = decoded.Length);
    }

    [Fact]
    public void OpenRejectsNullAndNegativeLimits()
    {
        using var source = CreateSource(new byte[] { 1 }, out var entry);
        using var verified = VerifiedExperienceSectionStream.Open(source, new object(), entry);

        Assert.Throws<ArgumentNullException>(() => BrotliExperienceSectionStream.Open(null!, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BrotliExperienceSectionStream.Open(verified, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BrotliExperienceSectionStream.Open(verified, 1, -1));
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


    private static MemoryStream CreateRawSource(byte[] compressed, out ExperienceSectionEntry entry, int logicalLength)
    {
        var source = new MemoryStream();
        source.SetLength(64 + compressed.Length);
        compressed.CopyTo(source.GetBuffer(), 64);
        entry = new ExperienceSectionEntry(1, 2, new byte[16], 64, compressed.Length,
            logicalLength, SHA256.HashData(compressed));
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
