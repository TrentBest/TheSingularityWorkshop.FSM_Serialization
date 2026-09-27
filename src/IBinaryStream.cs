namespace TheSingularityWorkshop.FSM_Serialization;

/// <summary>
/// Defines the byte-oriented stream boundary used by the serialization contracts.
/// </summary>
public interface IBinaryStream : IDisposable
{
    /// <summary>Gets a value indicating whether the stream supports reading.</summary>
    bool CanRead { get; }

    /// <summary>Gets a value indicating whether the stream supports writing.</summary>
    bool CanWrite { get; }

    /// <summary>Gets or sets the current position within the stream.</summary>
    long Position { get; set; }

    /// <summary>Gets the length of the stream in bytes.</summary>
    long Length { get; }

    /// <summary>Reads bytes from the stream into the supplied buffer.</summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <returns>The number of bytes read.</returns>
    int Read(Span<byte> buffer);

    /// <summary>Writes the supplied bytes to the stream.</summary>
    /// <param name="buffer">The bytes to write.</param>
    void Write(ReadOnlySpan<byte> buffer);

    /// <summary>Flushes buffered data to the underlying representation.</summary>
    void Flush();
}
