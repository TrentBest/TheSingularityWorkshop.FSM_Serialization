namespace TheSingularityWorkshop.FSM_Serialization;

public sealed class StreamBinaryStream : IBinaryStream
{
    private readonly Stream _stream;

    public StreamBinaryStream(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    public bool CanRead => _stream.CanRead;
    public bool CanWrite => _stream.CanWrite;

    public long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    public long Length => _stream.Length;

    public int Read(Span<byte> buffer) => _stream.Read(buffer);
    public void Write(ReadOnlySpan<byte> buffer) => _stream.Write(buffer);
    public void Flush() => _stream.Flush();
    public void Dispose() => _stream.Dispose();
}
