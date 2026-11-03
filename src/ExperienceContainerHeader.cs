using System.Buffers.Binary;
using System.Text;

namespace TheSingularityWorkshop.FSM_Serialization;

/// <summary>
/// Parses and validates the fixed version 1 Experience container header.
/// This internal boundary is intentionally provisional while the wire format is under review.
/// </summary>
internal readonly record struct ExperienceContainerHeader(
    ushort MajorVersion,
    ushort MinorVersion,
    uint EntryCount,
    long DirectoryOffset,
    long DirectoryLength,
    long FileLength)
{
    internal const int Size = 64;
    internal const int DirectoryEntrySize = 80;
    private static ReadOnlySpan<byte> Magic => "TSWEXP\0\0"u8;

    /// <summary>Reads the header from the beginning of a readable, seekable stream.</summary>
    internal static ExperienceContainerHeader Read(
        Stream source,
        long maximumContainerBytes = 2L * 1024 * 1024 * 1024,
        uint maximumDirectoryEntries = 4096)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!source.CanRead)
            throw new ArgumentException("The source stream must be readable.", nameof(source));
        if (!source.CanSeek)
            throw new ArgumentException("The source stream must be seekable.", nameof(source));
        if (maximumContainerBytes < Size)
            throw new ArgumentOutOfRangeException(nameof(maximumContainerBytes));
        if (maximumDirectoryEntries < 2)
            throw new ArgumentOutOfRangeException(nameof(maximumDirectoryEntries));

        long actualLength = source.Length;
        if (actualLength < Size)
            throw new InvalidDataException("The container is shorter than its fixed header.");
        if (actualLength > maximumContainerBytes)
            throw new InvalidDataException("The container exceeds the configured size limit.");

        Span<byte> bytes = stackalloc byte[Size];
        source.Position = 0;
        source.ReadExactly(bytes);

        if (!bytes[..8].SequenceEqual(Magic))
            throw new InvalidDataException("The container magic is invalid.");

        ushort major = BinaryPrimitives.ReadUInt16LittleEndian(bytes[8..]);
        ushort minor = BinaryPrimitives.ReadUInt16LittleEndian(bytes[10..]);
        uint headerSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..]);
        uint flags = BinaryPrimitives.ReadUInt32LittleEndian(bytes[16..]);
        uint entrySize = BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..]);
        uint entryCount = BinaryPrimitives.ReadUInt32LittleEndian(bytes[24..]);
        ulong directoryOffset = BinaryPrimitives.ReadUInt64LittleEndian(bytes[28..]);
        ulong directoryLength = BinaryPrimitives.ReadUInt64LittleEndian(bytes[36..]);
        ulong declaredFileLength = BinaryPrimitives.ReadUInt64LittleEndian(bytes[44..]);

        if (major != 1 || minor != 0)
            throw new InvalidDataException($"Unsupported container format version {major}.{minor}.");
        if (headerSize != Size || entrySize != DirectoryEntrySize)
            throw new InvalidDataException("The header or directory entry size is invalid.");
        if (flags != 0 || !IsAllZero(bytes[52..64]))
            throw new InvalidDataException("Reserved header flags or bytes are nonzero.");
        if (entryCount < 2 || entryCount > maximumDirectoryEntries)
            throw new InvalidDataException("The directory entry count is outside the configured range.");

        ulong expectedDirectoryLength = checked((ulong)entryCount * DirectoryEntrySize);
        if (directoryLength != expectedDirectoryLength)
            throw new InvalidDataException("The directory length does not match its entry count.");
        if (directoryOffset > long.MaxValue || directoryLength > long.MaxValue || declaredFileLength > long.MaxValue)
            throw new InvalidDataException("A declared container range exceeds supported stream offsets.");

        ulong directoryEnd;
        try
        {
            directoryEnd = checked(directoryOffset + directoryLength);
        }
        catch (OverflowException exception)
        {
            throw new InvalidDataException("The directory range overflows the file address space.", exception);
        }

        if ((directoryOffset & 7) != 0 || directoryOffset < Size)
            throw new InvalidDataException("The directory offset is misaligned or overlaps the header.");
        if (declaredFileLength != (ulong)actualLength || directoryEnd != declaredFileLength)
            throw new InvalidDataException("The declared file length or directory end does not match the source.");

        return new ExperienceContainerHeader(
            major,
            minor,
            entryCount,
            (long)directoryOffset,
            (long)directoryLength,
            actualLength);
    }

    private static bool IsAllZero(ReadOnlySpan<byte> bytes)
    {
        foreach (byte value in bytes)
        {
            if (value != 0)
                return false;
        }

        return true;
    }
}
