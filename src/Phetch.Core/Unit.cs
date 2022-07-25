namespace Phetch.Core;

/// <summary>
/// A type that allows only one value.
/// </summary>
/// <remarks>
/// This is used for implementing queries with no parameter and/or no return value.
/// </remarks>
public readonly struct Unit
{
    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is Unit;

    /// <inheritdoc />
    public override string ToString() => "()";

    internal bool Equals(Unit _) => true;
}
