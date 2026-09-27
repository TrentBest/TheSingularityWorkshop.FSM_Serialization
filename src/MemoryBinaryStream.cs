namespace TheSingularityWorkshop.FSM_Serialization;

/// <summary>
/// Provides an in-memory implementation of <see cref="IBinaryStream"/>.
/// </summary>
public class MemoryBinaryStream : IBinaryStream
{
    private readonly MemoryStream _stream;

    /// <summary>Initializes an empty memory-backed binary stream.</summary>
    public MemoryBinaryStream() : this(Array.Empty<byte>())
    {
    }

    /// <summary>Initializes a memory-backed binary stream containing the supplied data.</summary>
    /// <param name="data">The initial stream contents.</param>
    public MemoryBinaryStream(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _stream = new MemoryStream();
        if (data.Length > 0)
            _stream.Write(data, 0, data.Length);
        _stream.Position = 0;
    }

    /// <inheritdoc />
    public bool CanRead => true;

    /// <inheritdoc />
    public bool CanWrite => true;

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

    /// <summary>Returns the current contents of the stream as a byte array.</summary>
    /// <returns>A copy of the stream contents.</returns>
    public byte[] ToArray() => _stream.ToArray();

    /// <inheritdoc />
    public virtual void Dispose() => _stream.Dispose();
}
