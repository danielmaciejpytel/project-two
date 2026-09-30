using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

// The "tik" of the last seconds of a turn: the sound exists and the sound controller of the menu scene has it.
public class SoundAssetTests
{
    [Test]
    public void TheTickSoundExists()
    {
        Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, "Sounds/Tick.wav")));
    }

    [Test]
    public void TheMenuSceneGivesTheTickToTheSoundController()
    {
        string scene = File.ReadAllText(Path.Combine(Application.dataPath, "Scenes/MenuScene.unity"));

        Assert.IsTrue(Regex.IsMatch(scene, @"_tickClip: \{fileID: \d+, guid: [0-9a-f]{32}"), "_tickClip is empty in the sound controller");
    }
}
