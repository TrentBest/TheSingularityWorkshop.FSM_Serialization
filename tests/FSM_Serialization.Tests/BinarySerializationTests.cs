using System.Text;
using TheSingularityWorkshop.FSM_Serialization;

namespace FSM_Serialization.Tests;

public sealed class BinarySerializationTests
{
    [Fact]
    public void MemoryStreamStartsEmpty()
    {
        using var stream = new MemoryBinaryStream();

        Assert.True(stream.CanRead);
        Assert.True(stream.CanWrite);
        Assert.Equal(0, stream.Position);
        Assert.Equal(0, stream.Length);
        Assert.Empty(stream.ToArray());
    }

    [Fact]
    public void MemoryStreamRejectsNullInitialData()
    {
        Assert.Throws<ArgumentNullException>(() => new MemoryBinaryStream(null!));
    }

    [Fact]
    public void MemoryStreamCanReadInitialDataFromTheBeginning()
    {
        using var stream = new MemoryBinaryStream([1, 2, 3]);

        var buffer = new byte[3];

        Assert.Equal(3, stream.Read(buffer));
        Assert.Equal([1, 2, 3], buffer);
        Assert.Equal(3, stream.Position);
        Assert.Equal(3, stream.Length);
    }

    [Fact]
    public void MemoryStreamReturnsZeroAtEndOfInput()
    {
        using var stream = new MemoryBinaryStream([1, 2]);

        var buffer = new byte[2];

        Assert.Equal(2, stream.Read(buffer));
        Assert.Equal(0, stream.Read(buffer));
    }

    [Fact]
    public void MemoryStreamCanSeekAndOverwrite()
    {
        using var stream = new MemoryBinaryStream([1, 2, 3]);

        stream.Position = 1;
        stream.Write([9]);

        Assert.Equal([1, 9, 3], stream.ToArray());
        Assert.Equal(2, stream.Position);
    }

    [Fact]
    public void MemoryStreamCanPipeBytesToAnotherMemoryStream()
    {
        using var source = new MemoryBinaryStream([9, 8, 7, 6]);
        using var destination = new MemoryBinaryStream();

        var buffer = new byte[2];
        int read;

        while ((read = source.Read(buffer)) > 0)
            destination.Write(buffer.AsSpan(0, read));

        Assert.Equal([9, 8, 7, 6], destination.ToArray());
    }

    [Fact]
    public void PackAndUnpackRoundTrip()
    {
        var original = new TestValue(42);

        using var stream = new MemoryBinaryStream();
        original.Pack(stream);
        stream.Position = 0;

        var restored = new TestValue();
        restored.Unpack(stream);

        Assert.Equal(42, restored.Value);
    }

    [Fact]
    public void SerializableContractCombinesPackAndUnpack()
    {
        Assert.True(typeof(IBinaryPackable).IsAssignableFrom(typeof(IBinarySerializable)));
        Assert.True(typeof(IBinaryUnpackable).IsAssignableFrom(typeof(IBinarySerializable)));
    }

    [Fact]
    public void UnpackRejectsIncompleteInput()
    {
        using var stream = new MemoryBinaryStream([1, 2]);

        var value = new TestValue();

        Assert.Throws<EndOfStreamException>(() => value.Unpack(stream));
    }

    [Fact]
    public void StreamAdapterDelegatesCapabilitiesAndBytes()
    {
        using var backing = new MemoryStream([1, 2, 3]);
        using var stream = new StreamBinaryStream(backing);

        Assert.Equal(backing.CanRead, stream.CanRead);
        Assert.Equal(backing.CanWrite, stream.CanWrite);
        Assert.Equal(backing.Length, stream.Length);

        var buffer = new byte[3];
        Assert.Equal(3, stream.Read(buffer));
        Assert.Equal([1, 2, 3], buffer);

        stream.Position = 0;
        stream.Write([7]);

        Assert.Equal([7, 2, 3], backing.ToArray());
    }

    [Fact]
    public void StreamAdapterRejectsNullStream()
    {
        Assert.Throws<ArgumentNullException>(() => new StreamBinaryStream(null!));
    }

    private sealed class TestValue : IBinarySerializable
    {
        public TestValue()
        {
        }

        public TestValue(int value) => Value = value;

        public int Value { get; private set; }

        public void Pack(IBinaryStream stream)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            BitConverter.TryWriteBytes(bytes, Value);
            stream.Write(bytes);
        }

        public void Unpack(IBinaryStream stream)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];

            if (stream.Read(bytes) != bytes.Length)
                throw new EndOfStreamException();

            Value = BitConverter.ToInt32(bytes);
        }
    }
}
