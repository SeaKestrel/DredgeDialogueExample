using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Localization;
using Winch.Core;
using Winch.Serialization;
using Winch.Util;

namespace DredgeDialogueAPI
{
    internal class CharactersLoader : MonoBehaviour
    {
        public void Awake()
        {
            GameManager.Instance.OnGameStarted += LoadAssets;
        }

        internal static void LoadAssets()
        {
            WinchCore.Log.Debug("Loading characters...");

            string[] modDirs = Directory.GetDirectories("Mods");
            foreach (string modDir in modDirs)
            {
                string assetFolderPath = Path.Combine(modDir, "Assets");
                if (!Directory.Exists(assetFolderPath))
                    continue;
                string charactersFolderPath = Path.Combine(assetFolderPath, "Characters");

                if (Directory.Exists(charactersFolderPath)) LoadCharacters(charactersFolderPath);
            }
        }

        internal static void LoadCharacters(string charactersFolderPath)
        {
            string[] charactersFiles = Directory.GetFiles(charactersFolderPath);
            foreach (string file in charactersFiles)
            {
                try
                {
                    WinchCore.Log.Debug("Loading character");
                    AddItemFromMeta(file);
                }
                catch (Exception ex)
                {
                    WinchCore.Log.Error($"Failed to load character from {file}: {ex}");
                }
            }
        }

        internal static void AddItemFromMeta(string metaPath)
        {
            var meta = UtilHelpers.ParseMeta(metaPath);
            if (meta == null)
            {
                WinchCore.Log.Error($"Meta file {metaPath} is empty");
                return;
            }
            AdvancedSpeakerData speaker = UtilHelpers.GetScriptableObjectFromMeta<AdvancedSpeakerData>(meta, metaPath);

            if (PopulateObjectFromMeta(speaker, meta))
            {
                SpeakerData data = null;

                (GameManager.Instance.DialogueRunner.dialogueViews[0] as DredgeDialogueView).speakerDataLookup.lookupTable.TryGetValue(speaker.paralinguisticsNameKey, out data);
                if (data != null)
                {
                    speaker.paralinguistics = data.paralinguistics;
                }
                (GameManager.Instance.DialogueRunner.dialogueViews[0] as DredgeDialogueView).speakerDataLookup.lookupTable.Add(speaker.speakerNameKey, speaker);
            }

        }

        internal static bool PopulateObjectFromMeta(AdvancedSpeakerData speakerData, Dictionary<string, object> meta)
        {
            if (speakerData == null) throw new ArgumentNullException($"{nameof(speakerData)} is null");

            try
            {
                new SpeakerDataConverter().PopulateFields(speakerData, meta);
            }
            catch (Exception e)
            {
                WinchCore.Log.Error(e);
                return false;
            }
            return true;
        }

        internal class SpeakerDataConverter : DredgeTypeConverter<AdvancedSpeakerData>
        {

            private readonly Dictionary<string, FieldDefinition> _definitions = new()
            {
                { "id", new(null, o=> o.ToString()) },
                { "speakerNameKey", new(null, o=> o.ToString()) },
                { "paralinguisticsNameKey", new(null, o=> o.ToString()) },
                { "yarnRootNode", new(null, o=> o.ToString()) },
                { "smallPortraitSprite", new(null, o=> TextureUtil.GetSprite(o.ToString())) },
                { "portraitPrefab", new(null, o=> GameObject.Find(o.ToString())) },
            };

            public SpeakerDataConverter()
            {
                AddDefinitions(_definitions);
            }

            internal static LocalizedString CreateLocalizedString(string key, string value)
            {
                var keyValueTuple = (key, value);
                if (StringDefinitionCache.TryGetValue(keyValueTuple, out LocalizedString cached))
                {
                    return cached;
                }
                var localizedString = new LocalizedString(key, value);
                StringDefinitionCache.Add(keyValueTuple, localizedString);
                return localizedString;
            }
        }

        internal class AdvancedSpeakerData : SpeakerData
        {
            public string paralinguisticsNameKey;
        }
    }
}
