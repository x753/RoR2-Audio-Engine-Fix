using BepInEx;
using UnityEngine;
using System;
using System.Globalization;
using System.IO;
using HarmonyLib;
using RoR2;
using R2API.Utils;

namespace x753
{
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    [BepInPlugin("com.x753.AudioEngineFix", "Audio Engine Fix", "1.0.3")]
    public class AudioEngineFix : BaseUnityPlugin
    {
        public void Awake()
        {
            Debug.Log("Awake() has run in AudioEngineFix.cs");

            On.RoR2.UI.MainMenu.MainMenuController.Start += (orig, self) =>
            {
                orig(self);

                byte[] oldBytePattern = BytePatternUtilities.ConvertHexStringToByteArray("2000000000000000000000000101000000000000EB0000000100000005000000000000001F0000004869");

                byte[] newBytePattern = BytePatternUtilities.ConvertHexStringToByteArray("2000000000000000000000000001000000000000EB0000000100000005000000000000001F0000004869");

                byte[] allBytes = File.ReadAllBytes(Environment.CurrentDirectory + @"\Risk of Rain 2_Data\globalgamemanagers");
                // Replace the section of binary containing audio engine settings for the globalgamemanagers file in order to reenable the Unity Audio Engine.
                byte[] resultBytes = BytePatternUtilities.ReplaceBytes(allBytes, oldBytePattern, newBytePattern);

                if (resultBytes != null)
                {
                    File.WriteAllBytes(Environment.CurrentDirectory + @"\Risk of Rain 2_Data\globalgamemanagers", resultBytes);
                    GameObject restartWarning = new GameObject("RestartWarning");
                    restartWarning.AddComponent<RestartWarning>();
                    DontDestroyOnLoad(restartWarning);
                }
                else
                {
                    var harmony = new Harmony("com.x753.AudioEngineFix");
                    harmony.PatchAll();
                }
            };
        }
    }

    [HarmonyPatch(typeof(AkAudioListener), "OnEnable")]
    public class AkAudioListener_OnEnable
    {
        [HarmonyPostfix]
        public static void Postfix(AkAudioListener __instance)
        {
            if (__instance.gameObject.GetComponent<AudioListener>() == null)
                __instance.gameObject.AddComponent<AudioListener>();
            __instance.gameObject.GetComponent<AudioListener>().enabled = true;
        }
    }
    [HarmonyPatch(typeof(AkAudioListener), "OnDisable")]
    public class AkAudioListener_OnDisable
    {
        [HarmonyPostfix]
        public static void Postfix(AkAudioListener __instance)
        {
            if (__instance.gameObject.GetComponent<AudioListener>() == null)
                __instance.gameObject.AddComponent<AudioListener>();
            __instance.gameObject.GetComponent<AudioListener>().enabled = false;
        }
    }

    public class RestartWarning : MonoBehaviour
    {
        void OnGUI()
        {
            GUI.Label(new Rect(0f, 0f, 600f, 80f), "The Unity Audio Engine has been restored, please restart the game once for it to take effect.", GUI.skin.label);
        }
    }

    public static class BytePatternUtilities
    {
        public static byte[] ConvertHexStringToByteArray(string hexString)
        {
            byte[] data = new byte[hexString.Length / 2];
            for (int i = 0; i < data.Length; i++)
            {
                string byteValue = hexString.Substring(i * 2, 2);
                data[i] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;
        }

        private static int FindBytes(byte[] src, byte[] find)
        {
            int index = -1;
            int matchIndex = 0;
            for (int i = 0; i < src.Length; i++)
            {
                if (src[i] == find[matchIndex])
                {
                    if (matchIndex == (find.Length - 1))
                    {
                        index = i - matchIndex;
                        break;
                    }
                    matchIndex++;
                }
                else
                {
                    matchIndex = 0;
                }

            }
            return index;
        }

        public static byte[] ReplaceBytes(byte[] src, byte[] search, byte[] repl)
        {
            byte[] dst = null;
            byte[] temp = null;
            int index = FindBytes(src, search);
            while (index >= 0)
            {
                if (temp == null)
                    temp = src;
                else
                    temp = dst;

                dst = new byte[temp.Length - search.Length + repl.Length];

                Buffer.BlockCopy(temp, 0, dst, 0, index);
                Buffer.BlockCopy(repl, 0, dst, index, repl.Length);
                Buffer.BlockCopy(temp, index + search.Length, dst, index + repl.Length, temp.Length - (index + search.Length));

                index = FindBytes(dst, search);
            }
            return dst;
        }
    }
}
