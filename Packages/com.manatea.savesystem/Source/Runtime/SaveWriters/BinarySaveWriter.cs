using System;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using UnityEngine;

namespace Manatea.SaveSystem
{
    public class BinarySaveWriter : FileSaveWriter
    {
        public override bool Save(string identifier, SaveObject saveObject)
        {
            try
            {
                string filePath = BuildFilePath(identifier);
                SaveFile(filePath, saveObject);

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
                saveObject = (T)LoadFile(filePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e.InnerException);
                return false;
            }
        }


        protected override object LoadFile(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(stream);
            }
        }
        protected override void SaveFile(string path, object content)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, content);
            }
        }
    }
}
