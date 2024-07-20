using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace LC_TitleVideos;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private ConfigEntry<bool> configPlayAudio;
    private ConfigEntry<bool> configPlayDefaultVideos;
    private ConfigEntry<int> configLoopCount;

    private bool hasMultipleClips = false;
    private int loopCount = 0;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        SceneManager.sceneLoaded += OnSceneLoaded;

        configPlayAudio = Config.Bind("General",
                                "PlayAudio",
                                false,
                                "Plays the audio of the background videos.");

        configPlayDefaultVideos = Config.Bind("General",
                                "PlayDefaultVideos",
                                true,
                                "Plays any built-in videos in 'DefaultTitleVideos' folders.");

        configLoopCount = Config.Bind("General",
                                "LoopCount",
                                -1,
                                "Amount of times to loop videos before changing to a different one, set to -1 to disable alternating videos.");

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name.StartsWith("MainMenu"))
        {
            loopCount = 0;
            hasMultipleClips = false;

            GameObject mainButtons = GameObject.Find("MenuContainer");
            if (mainButtons != null)
            {
                GameObject playerGO = new GameObject("TitleVideo");
                playerGO.transform.parent = mainButtons.transform;

                RenderTexture texture = new RenderTexture(Screen.width, Screen.height, 16);

                AudioSource audioSource = playerGO.AddComponent<AudioSource>();

                VideoPlayer player = playerGO.AddComponent<VideoPlayer>();
                // player.clip = clip;
                player.url = PickRandomVideo();
                player.targetTexture = texture;
                player.isLooping = configLoopCount.Value <= -1;
                player.audioOutputMode = VideoAudioOutputMode.AudioSource;
                player.SetTargetAudioSource(0, audioSource);
                player.aspectRatio = VideoAspectRatio.FitInside;
                player.loopPointReached += OnLoopPointReached;
                player.Play();

                playerGO.transform.localPosition = new Vector3(0f, 0f, 0f);
                playerGO.transform.localScale = new Vector3(1f, 1f, 1f);
                playerGO.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                playerGO.transform.SetSiblingIndex(0);

                RectTransform t = playerGO.AddComponent<RectTransform>();
                t.anchorMin = new Vector2(0, 0);
                t.anchorMax = new Vector2(1, 1);
                t.sizeDelta = new Vector2(1, 1);

                RawImage image = playerGO.AddComponent<RawImage>();
                image.texture = texture;
                image.color = new Color(1f, 1f, 1f, 0.25f);
                
                if (!configPlayAudio.Value)
                {
                    audioSource.enabled = false;
                }
            }
        }
    }

    private void OnLoopPointReached(VideoPlayer vp)
    {
        if (configLoopCount.Value <= -1) return;

        loopCount++;
        if (loopCount <= configLoopCount.Value)
        {
            vp.Play();
            return;
        }

        string clip = vp.url;
        while (clip == vp.url && hasMultipleClips)
        {
            clip = PickRandomVideo();
        }

        loopCount = 0;

        vp.url = clip;
        vp.Play();
    }

    private string PickRandomVideo()
    {
        string[] dirs = Directory.GetDirectories(Paths.BepInExRootPath, "TitleVideos", SearchOption.AllDirectories);
        if (configPlayDefaultVideos.Value)
        {
            dirs = Directory.GetDirectories(Paths.BepInExRootPath, "DefaultTitleVideos", SearchOption.AllDirectories).Concat(dirs).ToArray();
        }

        List<FileInfo> infos = new List<FileInfo>();

        foreach (string dir in dirs)
        {
            DirectoryInfo d = new DirectoryInfo(dir);
            foreach (FileInfo f in d.GetFiles())
            {
                infos.Add(f);
            }
        }

        hasMultipleClips = infos.Count > 1;

        if (infos.Count > 0)
            return infos[Random.Range(0, infos.Count)].FullName;

        return "";
    }
}
