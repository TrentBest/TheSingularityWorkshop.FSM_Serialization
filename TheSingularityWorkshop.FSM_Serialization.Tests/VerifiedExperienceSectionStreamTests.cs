using System.Security.Cryptography;

namespace TheSingularityWorkshop.FSM_Serialization.Tests;

public sealed class VerifiedExperienceSectionStreamTests
{
    [Fact]
    public void OpenRejectsNullSourceGateAndEntry()
    {
        using var source = CreateSource();
        var entry = CreateEntries()[0];

        Assert.Throws<ArgumentNullException>(() => VerifiedExperienceSectionStream.Open(null!, new object(), entry));
        Assert.Throws<ArgumentNullException>(() => VerifiedExperienceSectionStream.Open(source, null!, entry));
        Assert.Throws<ArgumentNullException>(() => VerifiedExperienceSectionStream.Open(source, new object(), null!));
    }

    [Fact]
    public void OpenRejectsUnreadableAndNonSeekableSources()
    {
        using var source = CreateSource();
        using var nonSeekable = new NonSeekableStream(source.ToArray());
        var entry = CreateEntries()[0];
        source.Dispose();

        Assert.Throws<ArgumentException>(() => VerifiedExperienceSectionStream.Open(source, new object(), entry));
        Assert.Throws<ArgumentException>(() => VerifiedExperienceSectionStream.Open(nonSeekable, new object(), entry));
    }

    [Fact]
    public void OpenRejectsTruncatedPayload()
    {
        using var source = CreateSource();
        var entry = CreateEntries()[0];
        source.SetLength(entry.PayloadOffset + 2);

        Assert.Throws<EndOfStreamException>(() => VerifiedExperienceSectionStream.Open(source, new object(), entry));
    }

    [Fact]
    public void ReadAndSeekStayWithinVerifiedSection()
    {
        using var source = CreateSource();
        using var section = VerifiedExperienceSectionStream.Open(source, new object(), CreateEntries()[0]);

        Assert.True(section.CanRead);
        Assert.True(section.CanSeek);
        Assert.False(section.CanWrite);
        Assert.Equal(6, section.Length);
        Assert.Equal(0, section.Position);
        Assert.Equal(2, section.Read(new byte[2], 0, 2));
        Assert.Equal(2, section.Position);
        Assert.Equal(4, section.Seek(-2, SeekOrigin.End));
        Assert.Equal(2, section.Seek(-2, SeekOrigin.Current));
        Assert.Equal(1, section.Seek(1, SeekOrigin.Begin));
        Assert.Equal(5, section.Read(new byte[10]));
        Assert.Equal(6, section.Position);
        Assert.Equal(0, section.Read(new byte[1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => section.Position = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => section.Position = 7);
        Assert.Throws<IOException>(() => section.Seek(-1, SeekOrigin.Begin));
        Assert.Throws<IOException>(() => section.Seek(1, SeekOrigin.End));
        Assert.Throws<ArgumentOutOfRangeException>(() => section.Seek(0, (SeekOrigin)99));
    }

    [Fact]
    public void ReadValidatesBufferArguments()
    {
        using var source = CreateSource();
        using var section = VerifiedExperienceSectionStream.Open(source, new object(), CreateEntries()[0]);

        Assert.Throws<ArgumentNullException>(() => section.Read(null!, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => section.Read(new byte[2], -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => section.Read(new byte[2], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => section.Read(new byte[2], 1, 2));
    }

    [Fact]
    public void ReadThrowsIfSourceIsTruncatedAfterVerification()
    {
        using var source = CreateSource();
        using var section = VerifiedExperienceSectionStream.Open(source, new object(), CreateEntries()[0]);
        source.SetLength(64);

        Assert.Throws<EndOfStreamException>(() => section.Read(new byte[1]));
    }

    [Fact]
    public void DisposeDisablesCapabilitiesAndOperations()
    {
        using var source = CreateSource();
        var section = VerifiedExperienceSectionStream.Open(source, new object(), CreateEntries()[0]);
        section.Dispose();

        Assert.False(section.CanRead);
        Assert.False(section.CanSeek);
        Assert.False(section.CanWrite);
        Assert.Throws<ObjectDisposedException>(() => _ = section.Length);
        Assert.Throws<ObjectDisposedException>(() => _ = section.Position);
        Assert.Throws<ObjectDisposedException>(() => section.Position = 0);
        Assert.Throws<ObjectDisposedException>(() => section.Read(new byte[1]));
        Assert.Throws<ObjectDisposedException>(() => section.Seek(0, SeekOrigin.Begin));
        Assert.Throws<ObjectDisposedException>(() => section.Flush());
    }

    [Fact]
    public void MutatingOperationsAreNotSupported()
    {
        using var source = CreateSource();
        using var section = VerifiedExperienceSectionStream.Open(source, new object(), CreateEntries()[0]);

        Assert.Throws<NotSupportedException>(() => section.SetLength(0));
        Assert.Throws<NotSupportedException>(() => section.Write(new byte[1], 0, 1));
    }

    private static MemoryStream CreateSource()
    {
        var source = new MemoryStream();
        source.SetLength(80);
        byte[] payload = { 1, 2, 3, 4, 5, 6 };
        payload.CopyTo(source.GetBuffer(), 64);
        return source;
    }

    private static ExperienceSectionEntry[] CreateEntries()
    {
        byte[] payload = { 1, 2, 3, 4, 5, 6 };
        return
        [
            new ExperienceSectionEntry(ExperienceContainerDirectory.ManifestType, 0, new byte[16],
                64, payload.Length, payload.Length, SHA256.HashData(payload))
        ];
    }

    private sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
    }
}
