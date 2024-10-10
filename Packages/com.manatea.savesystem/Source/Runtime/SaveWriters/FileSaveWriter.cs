using System;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;

namespace Manatea.SaveSystem
{
    public class FileSaveWriter : SaveWriter
    {
        public virtual bool UsePrettyPrint => Debug.isDebugBuild;
        public virtual string DirectoryPath => Application.persistentDataPath + "/Saves/";
        public virtual string Extension => ".sav";

        public override bool Save(string identifier, SaveObject saveObject)
        {
            try
            {
                string filePath = BuildFilePath(identifier);
                string jsonString = JsonUtility.ToJson(saveObject, UsePrettyPrint);
                SaveFile(filePath, jsonString);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e.InnerException);
                return false;
            }
        }

        public override bool Load<T>(string identifier, out T saveObject)
        {
            saveObject = null;

            try
            {
                string filePath = BuildFilePath(identifier);
                string fileString = (string)LoadFile(filePath);
                saveObject = JsonUtility.FromJson<T>(fileString);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e.InnerException);
                return false;
            }
        }

        public override bool Rename(string identifier, string newIdentifier)
        {
            try
            {
                string filePath = BuildFilePath(identifier);
                string newFilePath = BuildFilePath(newIdentifier);
                MoveFile(filePath, newFilePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e.InnerException);
                return false;
            }
        }

        public override bool Delete(string identifier)
        {
            try
            {
                string filePath = BuildFilePath(identifier);
                DeleteFile(filePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e.InnerException);
                return false;
            }
        }

        public override string[] Find(Predicate<string> match)
        {
            string[] files = GetFiles(DirectoryPath);
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFileName(files[i]);
                files[i] = files[i].Remove(files[i].Length - Extension.Length);
            }
            return Array.FindAll(files, match);
        }


        protected virtual string BuildFilePath(string identifier)
        {
            return DirectoryPath + identifier + Extension;
        }
        protected virtual object LoadFile(string path)
        {
            return File.ReadAllText(path);
        }
        protected virtual void SaveFile(string path, object content)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                writer.Write(content);
            }
        }
        protected virtual void MoveFile(string path, string newPath)
        {
            File.Move(path, newPath);
        }
        protected virtual void DeleteFile(string path)
        {
            File.Delete(path);
        }
        protected virtual string[] GetFiles(string directoryPath)
        {
            return Directory.GetFiles(directoryPath, "*" + Extension);
        }
    }
}
