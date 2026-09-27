namespace TheSingularityWorkshop.FSM_Serialization;

public interface IBinaryStream : IDisposable
{
    bool CanRead { get; }
    bool CanWrite { get; }
    long Position { get; set; }
    long Length { get; }

    int Read(Span<byte> buffer);
    void Write(ReadOnlySpan<byte> buffer);
    void Flush();
}
