namespace TheSingularityWorkshop.FSM_Serialization;

/// <summary>
/// Provides reusable operations for reading complete fields from a binary stream.
/// </summary>
/// <remarks>
/// <see cref="IBinaryStream.Read(Span{byte})"/> follows ordinary stream semantics: a read may
/// return fewer bytes than requested. Format implementations that require a fixed-width field
/// should use <see cref="ReadExactly(IBinaryStream, Span{byte})"/> before decoding it.
/// This helper defines no byte order, field layout, allocation policy, or format version.
/// </remarks>
public static class BinaryStreamExtensions
{
    /// <summary>
    /// Reads until the destination is full or the stream reaches the end of its input.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="buffer">The destination buffer to fill.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="EndOfStreamException">
    /// The stream reaches the end before the destination buffer is full.
    /// </exception>
    /// <exception cref="IOException">
    /// The underlying stream reports an invalid read count.
    /// </exception>
    public static void ReadExactly(this IBinaryStream stream, Span<byte> buffer)
    {
        ArgumentNullException.ThrowIfNull(stream);

        while (!buffer.IsEmpty)
        {
            int bytesRead = stream.Read(buffer);

            if (bytesRead == 0)
            {
                throw new EndOfStreamException(
                    "The binary stream ended before the requested buffer was filled.");
            }

            if (bytesRead < 0 || bytesRead > buffer.Length)
            {
                throw new IOException(
                    $"The binary stream returned an invalid read count ({bytesRead}) for a buffer of {buffer.Length} bytes.");
            }

            buffer = buffer[bytesRead..];
        }
    }
}
