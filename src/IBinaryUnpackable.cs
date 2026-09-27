namespace TheSingularityWorkshop.FSM_Serialization;

/// <summary>
/// Defines a type that can reconstruct its state from an <see cref="IBinaryStream"/>.
/// </summary>
public interface IBinaryUnpackable
{
    /// <summary>Reads the type's binary representation from the supplied stream.</summary>
    /// <param name="stream">The source binary stream.</param>
    void Unpack(IBinaryStream stream);
}
