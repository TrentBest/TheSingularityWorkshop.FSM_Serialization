using System.Buffers.Binary;

namespace TheSingularityWorkshop.FSM_Serialization;

internal sealed record ExperienceSectionEntry(
    uint Type,
    uint Flags,
    byte[] Id,
    long PayloadOffset,
    long StoredLength,
    long LogicalLength,
    byte[] Sha256)
{
    internal bool IsRequired => (Flags & 1) != 0;
    internal uint Compression => (Flags >> 1) & 0x3;
}

internal sealed class ExperienceContainerDirectory
{
    internal const uint ManifestType = 1;
    internal const uint OverridesType = 2;
    internal const uint ResourceIndexType = 3;
    internal const uint ResourcePayloadType = 4;
    internal const uint OpaqueArtifactType = 5;
    internal const uint IntegrityMetadataType = 6;

    internal IReadOnlyList<ExperienceSectionEntry> Entries { get; }
    private readonly object _sourceGate = new();

    private ExperienceContainerDirectory(IReadOnlyList<ExperienceSectionEntry> entries)
        => Entries = entries;

    /// <summary>Verifies a section's stored bytes before returning a bounded read-only view.</summary>
    /// <remarks>The caller must keep the underlying source immutable for the lifetime of the returned stream.</remarks>
    internal VerifiedExperienceSectionStream OpenVerifiedSection(Stream source, ExperienceSectionEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!Entries.Contains(entry))
            throw new ArgumentException("The section entry does not belong to this directory.", nameof(entry));
        return VerifiedExperienceSectionStream.Open(source, _sourceGate, entry);
    }

    internal static ExperienceContainerDirectory Read(
        Stream source,
        ExperienceContainerHeader header,
        long maximumStoredSectionBytes = 512L * 1024 * 1024,
        long maximumLogicalSectionBytes = 512L * 1024 * 1024,
        long maximumAggregateLogicalBytes = 1024L * 1024 * 1024)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead || !source.CanSeek)
            throw new ArgumentException("The source stream must be readable and seekable.", nameof(source));
        if (maximumStoredSectionBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumStoredSectionBytes));
        if (maximumLogicalSectionBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumLogicalSectionBytes));
        if (maximumAggregateLogicalBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumAggregateLogicalBytes));

        int byteCount = checked((int)header.DirectoryLength);
        byte[] bytes = new byte[byteCount];
        source.Position = header.DirectoryOffset;
        source.ReadExactly(bytes);

        var entries = new List<ExperienceSectionEntry>(checked((int)header.EntryCount));
        for (int index = 0; index < header.EntryCount; index++)
        {
            ReadOnlySpan<byte> item = bytes.AsSpan(index * ExperienceContainerHeader.DirectoryEntrySize,
                ExperienceContainerHeader.DirectoryEntrySize);
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(item);
            uint flags = BinaryPrimitives.ReadUInt32LittleEndian(item[4..]);
            byte[] id = item.Slice(8, 16).ToArray();
            ulong offset = BinaryPrimitives.ReadUInt64LittleEndian(item[24..]);
            ulong stored = BinaryPrimitives.ReadUInt64LittleEndian(item[32..]);
            ulong logical = BinaryPrimitives.ReadUInt64LittleEndian(item[40..]);
            byte[] digest = item.Slice(48, 32).ToArray();

            if ((flags & 0xFFFFFFF8u) != 0 || ((flags >> 1) & 0x3) > 1)
                throw new InvalidDataException("A section contains reserved flags or an unsupported compression value.");
            if (offset > long.MaxValue || stored > long.MaxValue || logical > long.MaxValue)
                throw new InvalidDataException("A section range exceeds supported stream offsets.");
            if (stored > (ulong)maximumStoredSectionBytes || logical > (ulong)maximumLogicalSectionBytes)
                throw new InvalidDataException("A section exceeds its configured size limit.");
            if (((flags >> 1) & 0x3) == 0 && stored != logical)
                throw new InvalidDataException("An uncompressed section must have equal stored and logical lengths.");

            ulong end;
            try
            {
                end = checked(offset + stored);
            }
            catch (OverflowException exception)
            {
                throw new InvalidDataException("A section range overflows the file address space.", exception);
            }

            if ((offset & 7) != 0 || offset < ExperienceContainerHeader.Size || end > (ulong)header.DirectoryOffset)
                throw new InvalidDataException("A section payload is misaligned or overlaps the header/directory.");

            entries.Add(new ExperienceSectionEntry(type, flags, id, (long)offset,
                (long)stored, (long)logical, digest));
        }

        ValidateDirectoryOrder(entries);
        ValidateIdentityAndCardinality(entries);
        ValidatePayloadRangesAndPadding(source, entries, header.DirectoryOffset);

        long aggregateLogical = 0;
        foreach (ExperienceSectionEntry entry in entries)
        {
            try
            {
                aggregateLogical = checked(aggregateLogical + entry.LogicalLength);
            }
            catch (OverflowException exception)
            {
                throw new InvalidDataException("Aggregate logical section length overflows.", exception);
            }

            if (aggregateLogical > maximumAggregateLogicalBytes)
                throw new InvalidDataException("Aggregate logical section data exceeds its configured limit.");
        }

        return new ExperienceContainerDirectory(entries.AsReadOnly());
    }

    private static void ValidateDirectoryOrder(IReadOnlyList<ExperienceSectionEntry> entries)
    {
        for (int i = 1; i < entries.Count; i++)
        {
            ExperienceSectionEntry previous = entries[i - 1];
            ExperienceSectionEntry current = entries[i];
            int typeComparison = previous.Type.CompareTo(current.Type);
            if (typeComparison > 0 || (typeComparison == 0 && CompareIds(previous.Id, current.Id) >= 0))
                throw new InvalidDataException("Directory entries are not strictly sorted by section type and ID.");
        }
    }

    private static void ValidateIdentityAndCardinality(IReadOnlyList<ExperienceSectionEntry> entries)
    {
        int manifests = 0, integrity = 0, resourceIndexes = 0;
        var repeatedIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (ExperienceSectionEntry entry in entries)
        {
            bool zeroId = IsAllZero(entry.Id);
            switch (entry.Type)
            {
                case ManifestType:
                    manifests++;
                    RequireZeroId(zeroId);
                    break;
                case IntegrityMetadataType:
                    integrity++;
                    RequireZeroId(zeroId);
                    if (entry.Compression != 0)
                        throw new InvalidDataException("Integrity metadata must be uncompressed.");
                    break;
                case ResourceIndexType:
                    resourceIndexes++;
                    RequireZeroId(zeroId);
                    break;
                case OverridesType:
                case ResourcePayloadType:
                case OpaqueArtifactType:
                    if (zeroId)
                        throw new InvalidDataException("Repeated section types require nonzero section IDs.");
                    string key = entry.Type.ToString("X8", System.Globalization.CultureInfo.InvariantCulture) + Convert.ToHexString(entry.Id);
                    if (!repeatedIds.Add(key))
                        throw new InvalidDataException("A repeated section type contains a duplicate section ID.");
                    break;
                default:
                    if (entry.IsRequired)
                        throw new InvalidDataException($"Unknown required section type {entry.Type}.");
                    break;
            }
        }

        if (manifests != 1 || integrity != 1 || resourceIndexes > 1)
            throw new InvalidDataException("The directory violates version 1 singleton section cardinality.");
    }

    private static void ValidatePayloadRangesAndPadding(
        Stream source,
        IReadOnlyList<ExperienceSectionEntry> entries,
        long directoryOffset)
    {
        ExperienceSectionEntry[] physical = entries.OrderBy(entry => entry.PayloadOffset).ToArray();
        long cursor = ExperienceContainerHeader.Size;
        byte[] buffer = new byte[8192];

        foreach (ExperienceSectionEntry entry in physical)
        {
            if (entry.PayloadOffset < cursor)
                throw new InvalidDataException("Section payload ranges overlap.");
            EnsureZeroPadding(source, cursor, entry.PayloadOffset - cursor, buffer);
            cursor = checked(entry.PayloadOffset + entry.StoredLength);
        }

        if (cursor > directoryOffset)
            throw new InvalidDataException("A section payload overlaps the directory.");
        EnsureZeroPadding(source, cursor, directoryOffset - cursor, buffer);
    }

    private static void EnsureZeroPadding(Stream source, long offset, long length, byte[] buffer)
    {
        source.Position = offset;
        while (length > 0)
        {
            int requested = (int)Math.Min(buffer.Length, length);
            source.ReadExactly(buffer.AsSpan(0, requested));
            if (buffer.AsSpan(0, requested).IndexOfAnyExcept((byte)0) >= 0)
                throw new InvalidDataException("Container alignment padding must contain only zero bytes.");
            length -= requested;
        }
    }

    private static void RequireZeroId(bool isZero)
    {
        if (!isZero)
            throw new InvalidDataException("Singleton section types require the all-zero section ID.");
    }

    private static bool IsAllZero(ReadOnlySpan<byte> bytes)
        => bytes.IndexOfAnyExcept((byte)0) < 0;

    private static int CompareIds(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        => left.SequenceCompareTo(right);
}
