using TheSingularityWorkshop.FSM_Serialization;

namespace TheSingularityWorkshop.FSM_Serialization.Tests;

public sealed class BinaryStreamExtensionsTests
{
    [Fact]
    public void ReadExactlyFillsBufferAcrossShortReads()
    {
        using var backing = new OneByteAtATimeStream([1, 2, 3, 4]);
        using var stream = new StreamBinaryStream(backing);
        Span<byte> buffer = stackalloc byte[4];

        stream.ReadExactly(buffer);

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, buffer.ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ReadExactlyThrowsWhenInputEndsBeforeBufferIsFull(int availableBytes)
    {
        byte[] completeInput = [1, 2, 3];
        using var stream = new MemoryBinaryStream(completeInput[..availableBytes]);
        byte[] buffer = new byte[4];

        Assert.Throws<EndOfStreamException>(() => stream.ReadExactly(buffer));
    }

    [Fact]
    public void ReadExactlyAcceptsAnEmptyBufferWithoutConsumingInput()
    {
        using var stream = new MemoryBinaryStream([42]);

        stream.ReadExactly(Span<byte>.Empty);

        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void ReadExactlyRejectsNullStream()
    {
        IBinaryStream stream = null!;

        Assert.Throws<ArgumentNullException>(() => stream.ReadExactly(Span<byte>.Empty));
    }

    private sealed class OneByteAtATimeStream : MemoryStream
    {
        public OneByteAtATimeStream(byte[] bytes) : base(bytes)
        {
        }

        public override int Read(Span<byte> buffer)
            => base.Read(buffer[..Math.Min(1, buffer.Length)]);
    }
}
