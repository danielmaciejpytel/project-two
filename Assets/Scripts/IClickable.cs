using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IClickable
{
    void Click();
}

// Replaces OnMouseEnter/OnMouseExit, which depend on the legacy Input Manager.
public interface IHoverable
{
    void PointerEnter();
    void PointerExit();
}
