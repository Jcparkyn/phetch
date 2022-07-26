namespace Phetch.Core;

/// <summary>
/// A type that allows only one value.
/// </summary>
/// <remarks>
/// This is used for implementing queries with no parameter and/or no return value.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter")]
public readonly struct Unit
{
    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is Unit;

    /// <inheritdoc />
    public override string ToString() => "()";

    ///
    public static bool operator ==(Unit left, Unit right) => true;

    ///
    public static bool operator !=(Unit left, Unit right) => false;
}
