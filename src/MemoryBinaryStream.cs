namespace TheSingularityWorkshop.FSM_Serialization;

public class MemoryBinaryStream : IBinaryStream
{
    private readonly MemoryStream _stream;

    public MemoryBinaryStream() : this(Array.Empty<byte>())
    {
    }

    public MemoryBinaryStream(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _stream = new MemoryStream();
        if (data.Length > 0)
            _stream.Write(data, 0, data.Length);
        _stream.Position = 0;
    }

    public bool CanRead => true;
    public bool CanWrite => true;

    public long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    public long Length => _stream.Length;

    public int Read(Span<byte> buffer) => _stream.Read(buffer);
    public void Write(ReadOnlySpan<byte> buffer) => _stream.Write(buffer);
    public void Flush() => _stream.Flush();
    public byte[] ToArray() => _stream.ToArray();

    public virtual void Dispose() => _stream.Dispose();
}
