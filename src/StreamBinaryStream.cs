namespace TheSingularityWorkshop.FSM_Serialization;

/// <summary>
/// Adapts a standard .NET <see cref="Stream"/> to the <see cref="IBinaryStream"/> contract.
/// </summary>
public sealed class StreamBinaryStream : IBinaryStream
{
    private readonly Stream _stream;

    /// <summary>Initializes a stream adapter.</summary>
    /// <param name="stream">The underlying .NET stream.</param>
    public StreamBinaryStream(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    /// <inheritdoc />
    public bool CanRead => _stream.CanRead;

    /// <inheritdoc />
    public bool CanWrite => _stream.CanWrite;

    /// <inheritdoc />
    public long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    /// <inheritdoc />
    public long Length => _stream.Length;

    /// <inheritdoc />
    public int Read(Span<byte> buffer) => _stream.Read(buffer);

    /// <inheritdoc />
    public void Write(ReadOnlySpan<byte> buffer) => _stream.Write(buffer);

    /// <inheritdoc />
    public void Flush() => _stream.Flush();

    /// <inheritdoc />
    public void Dispose() => _stream.Dispose();
}
