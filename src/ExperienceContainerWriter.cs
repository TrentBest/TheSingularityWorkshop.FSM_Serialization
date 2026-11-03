using System.Buffers.Binary;
using System.Security.Cryptography;

namespace TheSingularityWorkshop.FSM_Serialization;

/// <summary>
/// Describes a repeatable section source for the provisional version 1 Experience writer.
/// OpenRead must return a fresh readable stream at position zero on every invocation.
/// </summary>
internal sealed record ExperienceSectionSource(
    uint Type,
    byte[] Id,
    bool Required,
    long Length,
    Func<Stream> OpenRead);

/// <summary>
/// Writes deterministic, uncompressed version 1 Experience containers using bounded buffers.
/// This is an internal prototype, not a frozen public API.
/// </summary>
internal static class ExperienceContainerWriter
{
    private const int BufferSize = 81920;
    private const long DefaultMaximumContainerBytes = 2L * 1024 * 1024 * 1024;
    private const long DefaultMaximumSectionBytes = 512L * 1024 * 1024;
    private const long DefaultMaximumAggregateLogicalBytes = 1024L * 1024 * 1024;
    private static ReadOnlySpan<byte> Magic => "TSWEXP\0\0"u8;

    private sealed record PlannedSection(
        ExperienceSectionSource Source,
        byte[] Digest,
        long Offset,
        int Ordinal);

    /// <summary>
    /// Writes a version 1 container. Sources are read twice: once to calculate their
    /// SHA-256 digests, and once to emit bytes while confirming the source did not change.
    /// The destination may be non-seekable; no section payload is buffered in memory.
    /// </summary>
    internal static void Write(
        Stream destination,
        IReadOnlyList<ExperienceSectionSource> sections,
        long maximumContainerBytes = DefaultMaximumContainerBytes,
        long maximumSectionBytes = DefaultMaximumSectionBytes,
        long maximumAggregateLogicalBytes = DefaultMaximumAggregateLogicalBytes)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(sections);
        if (!destination.CanWrite)
            throw new ArgumentException("The destination stream must be writable.", nameof(destination));
        if (destination.CanSeek && (destination.Position != 0 || destination.Length != 0))
            throw new ArgumentException("A seekable destination must be empty and positioned at the beginning of the container.", nameof(destination));
        if (maximumContainerBytes < ExperienceContainerHeader.Size)
            throw new ArgumentOutOfRangeException(nameof(maximumContainerBytes));
        if (maximumSectionBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumSectionBytes));
        if (maximumAggregateLogicalBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumAggregateLogicalBytes));
        if (sections.Count < 2 || sections.Count > 4096)
            throw new InvalidDataException("The section count is outside the version 1 directory range.");

        ValidateSources(sections, maximumSectionBytes, maximumAggregateLogicalBytes);

        var planned = new List<PlannedSection>(sections.Count);
        for (int index = 0; index < sections.Count; index++)
        {
            ExperienceSectionSource section = sections[index] with { Id = sections[index].Id.ToArray() };
            planned.Add(new PlannedSection(section, CalculateDigest(section), 0, index));
        }

        PlannedSection[] physical = planned
            .OrderBy(item => PhysicalOrder(item.Source.Type))
            .ThenBy(item => item.Source.Type)
            .ThenBy(item => item.Source.Id, ByteArrayComparer.Instance)
            .ToArray();

        long cursor = ExperienceContainerHeader.Size;
        var withOffsets = new Dictionary<int, PlannedSection>();
        foreach (PlannedSection item in physical)
        {
            cursor = Align8(cursor);
            withOffsets.Add(item.Ordinal, item with { Offset = cursor });
            cursor = checked(cursor + item.Source.Length);
        }

        long directoryOffset = Align8(cursor);
        long directoryLength = checked((long)sections.Count * ExperienceContainerHeader.DirectoryEntrySize);
        long fileLength = checked(directoryOffset + directoryLength);
        if (fileLength > maximumContainerBytes)
            throw new InvalidDataException("The completed container exceeds the configured size limit.");

        PlannedSection[] directory = planned
            .Select(item => withOffsets[item.Ordinal])
            .OrderBy(item => item.Source.Type)
            .ThenBy(item => item.Source.Id, ByteArrayComparer.Instance)
            .ToArray();

        WriteHeader(destination, (uint)directory.Length, directoryOffset, directoryLength, fileLength);

        cursor = ExperienceContainerHeader.Size;
        byte[] buffer = new byte[BufferSize];
        byte[] zeroes = new byte[8];
        foreach (PlannedSection item in physical)
        {
            WritePadding(destination, item.Offset - cursor, zeroes);
            WriteAndVerifySource(destination, item, buffer);
            cursor = checked(item.Offset + item.Source.Length);
        }

        WritePadding(destination, directoryOffset - cursor, zeroes);
        foreach (PlannedSection item in directory)
            WriteDirectoryEntry(destination, item);
    }

    private static void ValidateSources(
        IReadOnlyList<ExperienceSectionSource> sections,
        long maximumSectionBytes,
        long maximumAggregateLogicalBytes)
    {
        int manifests = 0, integrity = 0, resourceIndexes = 0;
        long aggregate = 0;
        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (ExperienceSectionSource section in sections)
        {
            ArgumentNullException.ThrowIfNull(section);
            ArgumentNullException.ThrowIfNull(section.Id);
            ArgumentNullException.ThrowIfNull(section.OpenRead);
            if (section.Id.Length != 16)
                throw new InvalidDataException("Every section ID must contain exactly 16 bytes.");
            if (section.Length < 0 || section.Length > maximumSectionBytes)
                throw new InvalidDataException("A section exceeds the configured size limit.");
            aggregate = checked(aggregate + section.Length);
            if (aggregate > maximumAggregateLogicalBytes)
                throw new InvalidDataException("Aggregate logical section data exceeds its configured limit.");

            bool zeroId = section.Id.AsSpan().IndexOfAnyExcept((byte)0) < 0;
            string directoryKey = section.Type.ToString("X8", System.Globalization.CultureInfo.InvariantCulture) + Convert.ToHexString(section.Id);
            if (!keys.Add(directoryKey))
                throw new InvalidDataException("The section set contains a duplicate directory key.");
            switch (section.Type)
            {
                case ExperienceContainerDirectory.ManifestType:
                    manifests++;
                    RequireZeroId(zeroId);
                    break;
                case ExperienceContainerDirectory.IntegrityMetadataType:
                    integrity++;
                    RequireZeroId(zeroId);
                    break;
                case ExperienceContainerDirectory.ResourceIndexType:
                    resourceIndexes++;
                    RequireZeroId(zeroId);
                    break;
                case ExperienceContainerDirectory.OverridesType:
                case ExperienceContainerDirectory.ResourcePayloadType:
                case ExperienceContainerDirectory.OpaqueArtifactType:
                    if (zeroId)
                        throw new InvalidDataException("Repeated section types require nonzero section IDs.");
                    break;
                default:
                    if (section.Required)
                        throw new InvalidDataException($"Unknown required section type {section.Type}.");
                    break;
            }
        }

        if (manifests != 1 || integrity != 1 || resourceIndexes > 1)
            throw new InvalidDataException("The section set violates version 1 singleton cardinality.");
    }

    private static byte[] CalculateDigest(ExperienceSectionSource section)
    {
        using Stream source = OpenSource(section);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[BufferSize];
        long remaining = section.Length;
        while (remaining > 0)
        {
            int read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read == 0)
                throw new EndOfStreamException("A section source ended before its declared length.");
            hash.AppendData(buffer, 0, read);
            remaining -= read;
        }
        if (source.ReadByte() != -1)
            throw new InvalidDataException("A section source contains more bytes than its declared length.");
        return hash.GetHashAndReset();
    }

    private static void WriteAndVerifySource(Stream destination, PlannedSection item, byte[] buffer)
    {
        using Stream source = OpenSource(item.Source);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long remaining = item.Source.Length;
        while (remaining > 0)
        {
            int read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read == 0)
                throw new EndOfStreamException("A section source ended before its declared length.");
            destination.Write(buffer, 0, read);
            hash.AppendData(buffer, 0, read);
            remaining -= read;
        }
        if (source.ReadByte() != -1)
            throw new InvalidDataException("A section source contains more bytes than its declared length.");
        if (!CryptographicOperations.FixedTimeEquals(hash.GetHashAndReset(), item.Digest))
            throw new InvalidDataException("A section source changed between digest calculation and writing.");
    }

    private static Stream OpenSource(ExperienceSectionSource section)
    {
        Stream source = section.OpenRead()
            ?? throw new InvalidDataException("A section source factory returned null.");
        if (!source.CanRead)
        {
            source.Dispose();
            throw new InvalidDataException("A section source stream must be readable.");
        }
        return source;
    }

    private static void WriteHeader(Stream destination, uint count, long directoryOffset, long directoryLength, long fileLength)
    {
        Span<byte> header = stackalloc byte[ExperienceContainerHeader.Size];
        header.Clear();
        Magic.CopyTo(header);
        BinaryPrimitives.WriteUInt16LittleEndian(header[8..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header[10..], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[12..], ExperienceContainerHeader.Size);
        BinaryPrimitives.WriteUInt32LittleEndian(header[20..], ExperienceContainerHeader.DirectoryEntrySize);
        BinaryPrimitives.WriteUInt32LittleEndian(header[24..], count);
        BinaryPrimitives.WriteUInt64LittleEndian(header[28..], (ulong)directoryOffset);
        BinaryPrimitives.WriteUInt64LittleEndian(header[36..], (ulong)directoryLength);
        BinaryPrimitives.WriteUInt64LittleEndian(header[44..], (ulong)fileLength);
        destination.Write(header);
    }

    private static void WriteDirectoryEntry(Stream destination, PlannedSection item)
    {
        Span<byte> entry = stackalloc byte[ExperienceContainerHeader.DirectoryEntrySize];
        entry.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(entry, item.Source.Type);
        BinaryPrimitives.WriteUInt32LittleEndian(entry[4..], item.Source.Required ? 1u : 0u);
        item.Source.Id.AsSpan().CopyTo(entry[8..24]);
        BinaryPrimitives.WriteUInt64LittleEndian(entry[24..], (ulong)item.Offset);
        BinaryPrimitives.WriteUInt64LittleEndian(entry[32..], (ulong)item.Source.Length);
        BinaryPrimitives.WriteUInt64LittleEndian(entry[40..], (ulong)item.Source.Length);
        item.Digest.AsSpan().CopyTo(entry[48..80]);
        destination.Write(entry);
    }

    private static void WritePadding(Stream destination, long length, byte[] zeroes)
    {
        while (length > 0)
        {
            int count = (int)Math.Min(zeroes.Length, length);
            destination.Write(zeroes, 0, count);
            length -= count;
        }
    }

    private static long Align8(long value) => checked((value + 7) & ~7L);

    private static int PhysicalOrder(uint type) => type switch
    {
        ExperienceContainerDirectory.ManifestType => 0,
        ExperienceContainerDirectory.IntegrityMetadataType => 1,
        ExperienceContainerDirectory.OverridesType => 2,
        ExperienceContainerDirectory.ResourceIndexType => 3,
        ExperienceContainerDirectory.ResourcePayloadType => 4,
        ExperienceContainerDirectory.OpaqueArtifactType => 5,
        _ => 6
    };

    private static void RequireZeroId(bool isZero)
    {
        if (!isZero)
            throw new InvalidDataException("Singleton section types require the all-zero section ID.");
    }

    private sealed class ByteArrayComparer : IComparer<byte[]>
    {
        internal static ByteArrayComparer Instance { get; } = new();
        public int Compare(byte[]? left, byte[]? right)
            => (left ?? Array.Empty<byte>()).AsSpan().SequenceCompareTo((right ?? Array.Empty<byte>()).AsSpan());
    }
}
