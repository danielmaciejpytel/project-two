using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animator controllers of the units live in Resources/UnitAnimators and are loaded on demand.
/// A unit prefab only stores the controller's name, so the draft and the game scene don't pull in
/// thousands of animation frames of units nobody picked. Controllers of picked units are requested
/// in the background as soon as they are chosen.
/// </summary>
public static class UnitAnimators
{
    private const string Folder = "UnitAnimators/";

    private static readonly Dictionary<string, ResourceRequest> Requests = new Dictionary<string, ResourceRequest>();

    // Starts loading in the background (nothing happens for empty names or controllers already requested).
    public static void Preload(string controllerName)
    {
        if (string.IsNullOrEmpty(controllerName) || Requests.ContainsKey(controllerName)) return;
        Requests[controllerName] = Resources.LoadAsync<RuntimeAnimatorController>(Folder + controllerName);
    }

    // Returns the controller, waiting for the background load if it hasn't finished yet.
    public static RuntimeAnimatorController Get(string controllerName)
    {
        if (string.IsNullOrEmpty(controllerName)) return null;
        Preload(controllerName);
        return Requests[controllerName].asset as RuntimeAnimatorController;
    }

    // Lets the loaded animations be freed when the game is left.
    public static void Release()
    {
        Requests.Clear();
    }
}
