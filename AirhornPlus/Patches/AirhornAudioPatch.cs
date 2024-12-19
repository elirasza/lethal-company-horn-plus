using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace AirhornPlus.Patches
{
  [HarmonyPatch(typeof(GrabbableObject))]
  class AirhornAudioPatch
  {
    protected readonly static List<string> audioFilenames = ["Fart", "Groal"];

    protected readonly static Dictionary<string, AudioClip> audioClips = [];

    protected readonly static string path = Path.Combine(Paths.PluginPath + "\\AirhornPlus\\");

    static AirhornAudioPatch()
    {
      audioFilenames.ForEach((audioFilename) =>
      {
        LoadAudioClip(audioFilename);
        LoadAudioClip(audioFilename + "Far");
      });
    }

    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    protected static void AudioPatch(GrabbableObject __instance)
    {
      ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("AirhornPlus");


      NoisemakerProp noisemakerProp = __instance.GetComponent<NoisemakerProp>();
      if (__instance != null && noisemakerProp != null && __instance.itemProperties.name == "Airhorn")
      {
        logger.LogMessage("An airhorn was found !");

        double random = new System.Random((int)noisemakerProp.NetworkObjectId).NextDouble();

        if (random > 0.8 && random <= 0.9)
        {
          noisemakerProp.noiseSFX[0] = audioClips["Fart"];
          noisemakerProp.noiseSFXFar[0] = audioClips["FartFar"];
        }
        else if (random > 0.9 && random <= 1)
        {
          noisemakerProp.noiseSFX[0] = audioClips["Groal"];
          noisemakerProp.noiseSFXFar[0] = audioClips["GroalFar"];
        }

        logger.LogMessage(noisemakerProp.noiseSFX[0].name);
        logger.LogMessage(noisemakerProp.noiseSFXFar[0].name);
      }
    }

    private static void LoadAudioClip(string name)
    {
      string filename = $"{Path.Combine(path, name)}.mp3";

      ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("Elirasza.AirhornPlus");
      UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(filename, (AudioType)13);
      request.SendWebRequest();

      while (!request.isDone) { }

      if (request.error != null)
      {
        logger.LogError($"Error loading sounds: {filename}\n{request.error}");
      }

      AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
      if (audioClip != null && audioClip.loadState == AudioDataLoadState.Loaded)
      {
        audioClip.name = Path.GetFileName(filename);
        audioClips[name] = audioClip;

        logger.LogInfo($"Loaded {filename}");
      }
    }
  }
}