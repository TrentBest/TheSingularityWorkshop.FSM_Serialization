using System.Security.Cryptography;

namespace TheSingularityWorkshop.FSM_Serialization;

/// <summary>Provides bounded, read-only access to a section after its stored bytes pass SHA-256 verification.</summary>
internal sealed class VerifiedExperienceSectionStream : Stream
{
    private readonly Stream _source;
    private readonly object _gate;
    private readonly long _offset;
    private readonly long _length;
    private long _position;
    private bool _disposed;

    private VerifiedExperienceSectionStream(Stream source, object gate, long offset, long length)
    {
        _source = source;
        _gate = gate;
        _offset = offset;
        _length = length;
    }

    internal static VerifiedExperienceSectionStream Open(Stream source, object gate, ExperienceSectionEntry entry)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(gate);
        ArgumentNullException.ThrowIfNull(entry);
        if (!source.CanRead || !source.CanSeek)
            throw new ArgumentException("The source stream must be readable and seekable.", nameof(source));

        byte[] digest;
        lock (gate)
        {
            source.Position = entry.PayloadOffset;
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            byte[] buffer = new byte[81920];
            long remaining = entry.StoredLength;
            while (remaining > 0)
            {
                int requested = (int)Math.Min(buffer.Length, remaining);
                int read = source.Read(buffer, 0, requested);
                if (read == 0)
                    throw new EndOfStreamException("The section payload ended before its declared length.");
                hash.AppendData(buffer, 0, read);
                remaining -= read;
            }
            digest = hash.GetHashAndReset();
        }

        if (!CryptographicOperations.FixedTimeEquals(digest, entry.Sha256))
            throw new InvalidDataException("The section SHA-256 digest does not match its directory entry.");

        return new VerifiedExperienceSectionStream(source, gate, entry.PayloadOffset, entry.StoredLength);
    }

    public override bool CanRead => !_disposed;
    public override bool CanSeek => !_disposed;
    public override bool CanWrite => false;
    public override long Length { get { ThrowIfDisposed(); return _length; } }
    public override long Position
    {
        get { ThrowIfDisposed(); return _position; }
        set { ThrowIfDisposed(); if (value < 0 || value > _length) throw new ArgumentOutOfRangeException(nameof(value)); _position = value; }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentOutOfRangeException();
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        int requested = (int)Math.Min(buffer.Length, _length - _position);
        if (requested == 0) return 0;
        int read;
        lock (_gate)
        {
            _source.Position = checked(_offset + _position);
            read = _source.Read(buffer[..requested]);
        }
        if (read == 0) throw new EndOfStreamException("The verified section source was truncated after verification.");
        _position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        long target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => checked(_position + offset),
            SeekOrigin.End => checked(_length + offset),
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };
        if (target < 0 || target > _length) throw new IOException("The requested position is outside the section.");
        _position = target;
        return target;
    }

    public override void Flush() { ThrowIfDisposed(); }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { _disposed = true; base.Dispose(disposing); }
    private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(VerifiedExperienceSectionStream)); }
}