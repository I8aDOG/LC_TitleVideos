using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mono.Cecil.Cil;

namespace LC_TitleVideos;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private ConfigEntry<bool> configPlayAudio;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        SceneManager.sceneLoaded += OnSceneLoaded;

        configPlayAudio = Config.Bind("General",
                                "PlayAudio",
                                false,
                                "Plays the audio of the background videos.");

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name.StartsWith("MainMenu"))
        {
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
                player.isLooping = true;
                player.audioOutputMode = VideoAudioOutputMode.AudioSource;
                player.SetTargetAudioSource(0, audioSource);
                player.aspectRatio = VideoAspectRatio.FitInside;
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

    private string PickRandomVideo()
    {
        string videoPath = Path.Combine(Paths.PluginPath, "LC_TitleVideos", "Videos");
        if (Directory.Exists(videoPath))
        {
            DirectoryInfo d = new DirectoryInfo(videoPath);
            FileInfo[] f = d.GetFiles();
            return f[Random.Range(0, f.Length)].FullName;
        }

        return "";
    }
}
