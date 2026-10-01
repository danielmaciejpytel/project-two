using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEffect
{
    string GetDescription();
}

// An effect that the tile a unit stands on puts on it. The details panel of a unit does not list it: that is information about the tile.
public interface ITileEffect : IEffect
{
}
