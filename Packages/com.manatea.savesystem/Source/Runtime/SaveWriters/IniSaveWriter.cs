using System;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection;
using IniParser;
using IniParser.Parser;
using IniParser.Model;
using UnityEngine;

namespace Manatea.SaveSystem
{
    public class IniSaveWriter : FileSaveWriter
    {
        public override string DirectoryPath => Application.dataPath.Remove(Application.dataPath.LastIndexOf('/')) + "/Saved/Saves/";
        public override string Extension => ".ini";

        public override bool Save(string identifier, SaveObject saveObject)
        {
            try
            {
                IniData iniData = new IniData();

                List<object> objectCache = new List<object>();
                Action<string, object> action = null;
                action = (string sectionName, object obj) =>
                {
                    if (obj == null)
                        return;
                    if (objectCache.Contains(obj))
                    {
                        Debug.LogWarning($"Recursion detected while parsing ini file ({ obj })");
                        return;
                    }
                    objectCache.Add(obj);

                    FieldInfo[] fields = obj.GetType().GetFields();
                    foreach (FieldInfo field in fields)
                    {
                        // Is the field of atomic type?
                        if (field.FieldType.IsPrimitive || field.FieldType == typeof(String))
                        {
                            string fieldSection = sectionName;
                            if (string.IsNullOrEmpty(fieldSection))
                                fieldSection = obj.GetType().Name;
                            if (!iniData.Sections.ContainsSection(fieldSection))
                                iniData.Sections.AddSection(fieldSection);

                            KeyData keyData = new KeyData(field.Name);
                            keyData.Value = field.GetValue(obj).ToString();
                            iniData.Sections[fieldSection].AddKey(keyData);
                        }
                        else
                        {
                            string recurSection = sectionName;
                            if (!string.IsNullOrEmpty(recurSection))
                                recurSection += ".";
                            recurSection += field.Name;
                            action.Invoke(recurSection, field.GetValue(obj));
                        }
                    }
                };

                action.Invoke("", saveObject);

                FileIniDataParser parser = new FileIniDataParser();
                string filePath = BuildFilePath(identifier);
                parser.WriteFile(filePath, iniData);

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
            saveObject = new T();

            try
            {
                string file = (string)LoadFile(BuildFilePath(identifier));

                IniDataParser parser = new IniDataParser();
                IniData parsedData = parser.Parse(file);

                foreach (var section in parsedData.Sections)
                {
                    string sec = section.SectionName;
                    string[] sections = sec.Split('.');

                    Type currentType = typeof(T);
                    object currentObject = saveObject;
                    bool parsingError = false;
                    if (sections.Length != 1 || sections[0] != typeof(T).Name)
                    {
                        for (int i = 0; i < sections.Length; i++)
                        {
                            FieldInfo field = currentType.GetField(sections[i]);
                            if (field == null)
                            {
                                parsingError = true;
                                break;
                            }

                            currentType = field.FieldType;
                            object newObject = field.GetValue(currentObject);
                            if (newObject == null)
                            {
                                newObject = Activator.CreateInstance(currentType);
                                field.SetValue(currentObject, newObject);
                            }
                            currentObject = newObject;
                        }
                    }
                    
                    if (parsingError)
                        continue;

                    foreach (var key in section.Keys)
                    {
                        FieldInfo field = currentType.GetField(key.KeyName);
                        if (field == null)
                            continue;

                        TypeConverter converter = TypeDescriptor.GetConverter(field.FieldType);
                        if (converter.IsValid(key.Value))
                        {
                            object valueObj = converter.ConvertFrom(null, CultureInfo.InvariantCulture, key.Value);
                            field.SetValue(currentObject, valueObj);
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e.InnerException);
                return false;
            }
        }
    }
}
