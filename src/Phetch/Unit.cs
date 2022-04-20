namespace Phetch;

public readonly struct Unit
{
    public override int GetHashCode() => 0;
    public override bool Equals(object obj) => obj is Unit;
    public bool Equals(Unit _) => true;
}
