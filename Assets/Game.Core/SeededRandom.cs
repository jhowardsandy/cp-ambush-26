#nullable enable

using System;

namespace TacticalStrategyGame.Core
{

/// <summary>A small, explicit deterministic generator for simulation-owned randomness.</summary>
public sealed class SeededRandom
{
    private uint _state;

    public SeededRandom(uint seed) => _state = seed;

    public uint NextUInt32()
    {
        _state = unchecked((_state * 1664525u) + 1013904223u);
        return _state;
    }

    public int Next(int exclusiveMaximum)
    {
        if (exclusiveMaximum <= 0)
            throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));

        return (int)(NextUInt32() % (uint)exclusiveMaximum);
    }
}

}
