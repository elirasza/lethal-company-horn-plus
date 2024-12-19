using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace AirhornPlus.Patches
{
  [HarmonyPatch(typeof(GrabbableObject))]
  class AirhornAudioPatch
  {
    protected readonly static List<string> audioFilenames = ["Fart", "Groal", "MLG"];
    protected readonly static Dictionary<string, (AudioClip?, AudioClip?)> audioClips = [];
    protected readonly static string path = Path.Combine(Paths.PluginPath + "\\AirhornPlus\\");

    public static ManualLogSource Logger { get; protected set; } = BepInEx.Logging.Logger.CreateLogSource("Elirasza.AirhornPlus.AirhornAudioPatch");

    static AirhornAudioPatch()
    {
      audioFilenames.ForEach((audioFilename) => audioClips[audioFilename] = (LoadAudioClip(audioFilename), LoadAudioClip(audioFilename + "Far")));
    }

    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    protected static void Patch(GrabbableObject __instance)
    {
      NoisemakerProp noisemakerProp = __instance.GetComponent<NoisemakerProp>();
      if (__instance != null && noisemakerProp != null && __instance.itemProperties.name == "Airhorn")
      {
        ChangeAirhornSound(noisemakerProp);
      }
    }

    private static void ChangeAirhornSound(NoisemakerProp airhorn)
    {
      double random = new System.Random((int)airhorn.NetworkObjectId).NextDouble();
      if (random > 0.5)
      {
        KeyValuePair<string, (AudioClip?, AudioClip?)> pair = audioClips.ElementAt((int)Math.Round((random - 0.5) * 2 * (audioClips.Count - 1)));
        airhorn.noiseSFX[0] = pair.Value.Item1;
        airhorn.noiseSFXFar[0] = pair.Value.Item2;

        Logger.LogMessage($"Found airhorn {airhorn.NetworkObjectId}, replaced the sound by {pair.Key}.");
      }
      else
      {
        Logger.LogMessage($"Found an airhorn {airhorn.NetworkObjectId}, kept the original sound.");
      }

      Logger.LogMessage(airhorn.noiseSFX[0].name);
      Logger.LogMessage(airhorn.noiseSFXFar[0].name);
    }

    private static AudioClip? LoadAudioClip(string name)
    {
      string filename = $"{Path.Combine(path, name)}.mp3";

      UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(filename, (AudioType)13);
      request.SendWebRequest();

      while (!request.isDone) { }

      if (request.error != null)
      {
        Logger.LogError($"Error loading sounds: {filename}\n{request.error}");
      }

      AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
      if (audioClip != null && audioClip.loadState == AudioDataLoadState.Loaded)
      {
        Logger.LogInfo($"Loaded {filename}");
        audioClip.name = Path.GetFileName(filename);
        return audioClip;
      }
      else
      {
        return null;
      }
    }
  }
}