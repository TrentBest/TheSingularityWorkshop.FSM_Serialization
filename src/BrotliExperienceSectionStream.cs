using System.Buffers;
using System.IO.Compression;

namespace TheSingularityWorkshop.FSM_Serialization;

/// <summary>Incrementally decodes a SHA-256-verified Brotli section with a fixed memory bound.</summary>
/// <remarks>
/// The stored source must remain immutable for this stream's lifetime. The complete compressed
/// payload is verified before this decoder is constructed; decoding itself is lazy.
/// </remarks>
internal sealed class BrotliExperienceSectionStream : Stream
{
    private const int BufferSize = 81920;
    private readonly VerifiedExperienceSectionStream _stored;
    private BrotliDecoder _decoder = new();
    private readonly long _logicalLength;
    private readonly byte[] _input = new byte[BufferSize];
    private int _inputOffset;
    private int _inputCount;
    private long _logicalPosition;
    private bool _completed;
    private bool _disposed;

    private BrotliExperienceSectionStream(VerifiedExperienceSectionStream stored, long logicalLength)
    {
        _stored = stored;
        _logicalLength = logicalLength;
    }

    internal static BrotliExperienceSectionStream Open(
        VerifiedExperienceSectionStream stored,
        long logicalLength,
        long maximumLogicalBytes)
    {
        ArgumentNullException.ThrowIfNull(stored);
        if (logicalLength < 0)
            throw new ArgumentOutOfRangeException(nameof(logicalLength));
        if (maximumLogicalBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumLogicalBytes));
        if (logicalLength > maximumLogicalBytes)
            throw new InvalidDataException("The declared logical section length exceeds the configured decompression limit.");
        return new BrotliExperienceSectionStream(stored, logicalLength);
    }

    public override bool CanRead => !_disposed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length { get { ThrowIfDisposed(); return _logicalLength; } }
    public override long Position
    {
        get { ThrowIfDisposed(); return _logicalPosition; }
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (offset < 0 || count < 0 || offset > buffer.Length - count)
            throw new ArgumentOutOfRangeException();
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        if (buffer.IsEmpty || _completed)
            return 0;

        int totalWritten = 0;
        Span<byte> output = buffer;
        Span<byte> overflowProbe = stackalloc byte[1];

        while (true)
        {
            bool probingOverflow = _logicalPosition == _logicalLength;
            Span<byte> destination = probingOverflow
                ? overflowProbe
                : output[..(int)Math.Min(output.Length, _logicalLength - _logicalPosition)];

            if (_inputOffset == _inputCount && _stored.Position < _stored.Length)
                RefillInput();

            ReadOnlySpan<byte> input = _input.AsSpan(_inputOffset, _inputCount - _inputOffset);
            OperationStatus status = _decoder.Decompress(input, destination, out int consumed, out int written);
            _inputOffset += consumed;

            if (written > 0)
            {
                if (probingOverflow)
                    throw new InvalidDataException("Brotli output exceeds the declared logical section length.");
                _logicalPosition = checked(_logicalPosition + written);
                totalWritten += written;
                output = output[written..];
            }

            if (status == OperationStatus.InvalidData)
                throw new InvalidDataException("The Brotli section payload is malformed.");

            if (status == OperationStatus.Done)
            {
                if (_logicalPosition != _logicalLength)
                    throw new InvalidDataException("Brotli output does not match the declared logical section length.");
                if (_inputOffset != _inputCount || _stored.Position != _stored.Length)
                    throw new InvalidDataException("The Brotli section contains trailing stored bytes.");
                _completed = true;
                _decoder.Dispose();
                return totalWritten;
            }

            if (status == OperationStatus.DestinationTooSmall)
            {
                if (totalWritten > 0 && _logicalPosition < _logicalLength)
                    return totalWritten;
                continue;
            }

            // NeedMoreData: retain any unconsumed input and append more bytes.
            if (_stored.Position == _stored.Length && _inputOffset == _inputCount)
                throw new InvalidDataException("The Brotli section ended before the compressed stream completed.");

            if (_inputOffset > 0)
            {
                int remaining = _inputCount - _inputOffset;
                if (remaining > 0)
                    Buffer.BlockCopy(_input, _inputOffset, _input, 0, remaining);
                _inputOffset = 0;
                _inputCount = remaining;
            }

            if (_inputCount == _input.Length)
                throw new InvalidDataException("The Brotli decoder cannot make progress within its bounded input buffer.");

            int read = _stored.Read(_input, _inputCount, _input.Length - _inputCount);
            if (read == 0)
                throw new InvalidDataException("The Brotli section ended before the compressed stream completed.");
            _inputCount += read;

            if (totalWritten > 0 && output.IsEmpty && _logicalPosition < _logicalLength)
                return totalWritten;
        }
    }

    private void RefillInput()
    {
        _inputOffset = 0;
        _inputCount = _stored.Read(_input, 0, _input.Length);
        if (_inputCount == 0)
            throw new InvalidDataException("The Brotli section ended before the compressed stream completed.");
    }

    public override void Flush() { ThrowIfDisposed(); }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _decoder.Dispose();
            _stored.Dispose();
        }
        _disposed = true;
        base.Dispose(disposing);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BrotliExperienceSectionStream));
    }
}
