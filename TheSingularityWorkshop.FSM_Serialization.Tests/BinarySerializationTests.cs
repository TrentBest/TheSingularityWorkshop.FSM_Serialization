using System.Buffers.Binary;
using TheSingularityWorkshop.FSM_Serialization;

namespace TheSingularityWorkshop.FSM_Serialization.Tests;

public sealed class BinarySerializationTests
{
    [Fact]
    public void MemoryBinaryStreamStartsEmptyAndIsReadableAndWritable()
    {
        using var stream = new MemoryBinaryStream();

        Assert.True(stream.CanRead);
        Assert.True(stream.CanWrite);
        Assert.Equal(0, stream.Length);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void MemoryBinaryStreamWritesReadsAndPreservesBytes()
    {
        using var stream = new MemoryBinaryStream();
        byte[] expected = [1, 2, 3, 4];

        stream.Write(expected);
        stream.Flush();

        Assert.Equal(expected.Length, stream.Length);
        Assert.Equal(expected, stream.ToArray());

        stream.Position = 0;
        byte[] actual = new byte[expected.Length];

        Assert.Equal(expected.Length, stream.Read(actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MemoryBinaryStreamCanStartFromExistingBytes()
    {
        byte[] expected = [9, 8, 7, 6];
        using var stream = new MemoryBinaryStream(expected);

        Assert.Equal(expected, stream.ToArray());
        Assert.Equal(0, stream.Position);

        byte[] actual = new byte[expected.Length];
        Assert.Equal(expected.Length, stream.Read(actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MemoryBinaryStreamRejectsNullInitialData()
    {
        Assert.Throws<ArgumentNullException>(() => new MemoryBinaryStream(null!));
    }

    [Fact]
    public void MemoryBinaryStreamCopiesInitialData()
    {
        byte[] input = [1, 2, 3];
        using var stream = new MemoryBinaryStream(input);

        input[0] = 99;

        Assert.Equal(new byte[] { 1, 2, 3 }, stream.ToArray());
    }

    [Fact]
    public void MemoryBinaryStreamReturnsZeroAtEndOfInput()
    {
        using var stream = new MemoryBinaryStream([1, 2]);
        stream.Position = stream.Length;

        Span<byte> buffer = stackalloc byte[1];

        Assert.Equal(0, stream.Read(buffer));
    }

    [Fact]
    public void MemoryBinaryStreamCanSeekAndOverwrite()
    {
        using var stream = new MemoryBinaryStream([1, 2, 3]);

        stream.Position = 1;
        stream.Write([9]);

        Assert.Equal(new byte[] { 1, 9, 3 }, stream.ToArray());
    }

    [Fact]
    public void StreamBinaryStreamDelegatesCapabilitiesAndBytes()
    {
        using var backing = new MemoryStream([5, 4, 3, 2]);
        using var stream = new StreamBinaryStream(backing);

        Assert.Equal(backing.CanRead, stream.CanRead);
        Assert.Equal(backing.CanWrite, stream.CanWrite);
        Assert.Equal(backing.Length, stream.Length);

        byte[] buffer = new byte[4];
        Assert.Equal(4, stream.Read(buffer));
        Assert.Equal(new byte[] { 5, 4, 3, 2 }, buffer);
    }

    [Fact]
    public void StreamBinaryStreamDelegatesPositionReadsAndWrites()
    {
        using var backing = new MemoryStream([1, 2, 3, 4]);
        using var stream = new StreamBinaryStream(backing);

        backing.Position = 2;
        Assert.Equal(2, stream.Position);

        stream.Position = 1;
        Assert.Equal(1, backing.Position);
    }

    [Fact]
    public void StreamBinaryStreamExposesShortReadsWithoutFillingTheBuffer()
    {
        using var backing = new PartialReadMemoryStream([1, 2, 3, 4]);
        using var stream = new StreamBinaryStream(backing);
        Span<byte> buffer = stackalloc byte[4];

        Assert.Equal(1, stream.Read(buffer));
        Assert.Equal((byte)1, buffer[0]);
    }

    [Fact]
    public void StreamBinaryStreamPreservesUnsupportedUnderlyingCapabilities()
    {
        using var backing = new NonSeekableReadOnlyStream();
        using var stream = new StreamBinaryStream(backing);

        Assert.True(stream.CanRead);
        Assert.False(stream.CanWrite);
        Assert.Throws<NotSupportedException>(() => _ = stream.Position);
        Assert.Throws<NotSupportedException>(() => _ = stream.Length);
        Assert.Throws<NotSupportedException>(() => stream.Write(new byte[] { 1 }));
    }

    [Fact]
    public void StreamBinaryStreamRejectsNullStream()
    {
        Assert.Throws<ArgumentNullException>(() => new StreamBinaryStream(null!));
    }

    [Fact]
    public void StreamBinaryStreamDisposesTheSuppliedStream()
    {
        var backing = new TrackingMemoryStream();
        var stream = new StreamBinaryStream(backing);

        stream.Dispose();

        Assert.True(backing.WasDisposed);
    }

    [Fact]
    public void StreamBinaryStreamWritesThroughToBackingStream()
    {
        using var backing = new MemoryStream();
        using (var stream = new StreamBinaryStream(backing))
        {
            stream.Write([7, 8, 9]);
            stream.Flush();
        }

        Assert.Equal(new byte[] { 7, 8, 9 }, backing.ToArray());
    }

    [Fact]
    public void BinaryPackableAndUnpackableRoundTrip()
    {
        var original = new TestValue(123456789);

        using var stream = new MemoryBinaryStream();
        original.Pack(stream);

        stream.Position = 0;

        var restored = new TestValue();
        restored.Unpack(stream);

        Assert.Equal(original.Value, restored.Value);
    }

    [Fact]
    public void TestValuePacksInt32AsLittleEndianGoldenBytes()
    {
        var value = new TestValue(0x12345678);
        using var stream = new MemoryBinaryStream();

        value.Pack(stream);

        Assert.Equal(new byte[] { 0x78, 0x56, 0x34, 0x12 }, stream.ToArray());
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void TestValueRoundTripsInt32BoundaryValues(int originalValue)
    {
        var original = new TestValue(originalValue);
        using var stream = new MemoryBinaryStream();

        original.Pack(stream);
        stream.Position = 0;

        var restored = new TestValue();
        restored.Unpack(stream);

        Assert.Equal(originalValue, restored.Value);
    }

    [Fact]
    public void UnpackRejectsEveryTruncatedInt32Prefix()
    {
        byte[] completeValue = [0x78, 0x56, 0x34, 0x12];

        for (int length = 0; length < completeValue.Length; length++)
        {
            using var stream = new MemoryBinaryStream(completeValue[..length]);
            var value = new TestValue();

            Assert.Throws<EndOfStreamException>(() => value.Unpack(stream));
        }
    }

    [Fact]
    public void BinarySerializableCombinesPackAndUnpackContracts()
    {
        IBinarySerializable value = new TestValue(42);

        using var stream = new MemoryBinaryStream();
        value.Pack(stream);

        stream.Position = 0;

        var restored = new TestValue();
        restored.Unpack(stream);

        Assert.Equal(42, restored.Value);
    }

    [Fact]
    public void UnpackRejectsIncompleteBinaryInput()
    {
        using var stream = new MemoryBinaryStream([1, 2, 3]);
        var value = new TestValue();

        Assert.Throws<EndOfStreamException>(() => value.Unpack(stream));
    }

    [Fact]
    public void MemoryBinaryStreamEmptyReadsAndWritesAreNoOps()
    {
        using var stream = new MemoryBinaryStream([1, 2]);

        Assert.Equal(0, stream.Read(Span<byte>.Empty));
        stream.Write(ReadOnlySpan<byte>.Empty);

        Assert.Equal(2, stream.Length);
        Assert.Equal(new byte[] { 1, 2 }, stream.ToArray());
    }

    [Fact]
    public void MemoryBinaryStreamCanWriteAfterSeekingPastEnd()
    {
        using var stream = new MemoryBinaryStream([1, 2]);

        stream.Position = 4;
        stream.Write([9]);

        Assert.Equal(new byte[] { 1, 2, 0, 0, 9 }, stream.ToArray());
        Assert.Equal(5, stream.Length);
    }

    [Fact]
    public void MemoryBinaryStreamRejectsNegativePosition()
    {
        using var stream = new MemoryBinaryStream();

        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Position = -1);
    }

    [Fact]
    public void MemoryBinaryStreamOperationsFailAfterDisposal()
    {
        var stream = new MemoryBinaryStream();
        stream.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = stream.Length);
        Assert.Throws<ObjectDisposedException>(() => stream.Read(new byte[1]));
    }

    [Fact]
    public void StreamBinaryStreamDelegatesFlush()
    {
        using var backing = new TrackingMemoryStream();
        using var stream = new StreamBinaryStream(backing);

        stream.Flush();

        Assert.True(backing.WasFlushed);
    }

    [Fact]
    public void UnpackCanCompleteAFieldAcrossMultipleShortReads()
    {
        using var backing = new PartialReadMemoryStream([0x78, 0x56, 0x34, 0x12]);
        using var stream = new StreamBinaryStream(backing);
        var value = new TestValue();

        value.Unpack(stream);

        Assert.Equal(0x12345678, value.Value);
    }

    private sealed class PartialReadMemoryStream : MemoryStream
    {
        public PartialReadMemoryStream(byte[] data) : base(data)
        {
        }

        public override int Read(Span<byte> buffer)
            => base.Read(buffer[..Math.Min(1, buffer.Length)]);
    }

    private sealed class NonSeekableReadOnlyStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override int Read(Span<byte> buffer) => 0;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();
    }

    private sealed class TrackingMemoryStream : MemoryStream
    {
        public bool WasDisposed { get; private set; }
        public bool WasFlushed { get; private set; }

        public override void Flush()
        {
            WasFlushed = true;
            base.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class TestValue : IBinarySerializable
    {
        public TestValue()
        {
        }

        public TestValue(int value)
        {
            Value = value;
        }

        public int Value { get; private set; }

        public void Pack(IBinaryStream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, Value);
            stream.Write(buffer);
        }

        public void Unpack(IBinaryStream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            int totalRead = 0;

            while (totalRead < buffer.Length)
            {
                int bytesRead = stream.Read(buffer[totalRead..]);
                if (bytesRead == 0)
                    throw new EndOfStreamException();

                totalRead += bytesRead;
            }

            Value = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }
    }
}
