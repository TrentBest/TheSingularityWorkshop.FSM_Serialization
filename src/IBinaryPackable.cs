namespace TheSingularityWorkshop.FSM_Serialization;

/// <summary>
/// Defines a type that can write its binary representation to an <see cref="IBinaryStream"/>.
/// </summary>
public interface IBinaryPackable
{
    /// <summary>Writes the type's binary representation to the supplied stream.</summary>
    /// <param name="stream">The destination binary stream.</param>
    void Pack(IBinaryStream stream);
}
