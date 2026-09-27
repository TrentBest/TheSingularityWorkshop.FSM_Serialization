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
    public void StreamBinaryStreamRejectsNullStream()
    {
        Assert.Throws<ArgumentNullException>(() => new StreamBinaryStream(null!));
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

            if (stream.Read(buffer) != buffer.Length)
                throw new EndOfStreamException();

            Value = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }
    }
}
