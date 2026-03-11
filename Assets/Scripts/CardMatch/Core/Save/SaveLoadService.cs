using System.IO;
using UnityEngine;

namespace CardMatch.Core.Save
{
    public class SaveLoadService
    {
        private const string FileName = "cardmatch_save.json";

        private string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, FileName); }
        }

        public bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public void Save(GameSaveData data)
        {
            if (data == null)
            {
                return;
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }

        public GameSaveData Load()
        {
            if (!HasSave())
            {
                return null;
            }

            string json = File.ReadAllText(SavePath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            return data;
        }

        public void DeleteSave()
        {
            if (!HasSave())
            {
                return;
            }

            File.Delete(SavePath);
        }
    }
}