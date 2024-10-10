using System;
using UnityEngine;

namespace Manatea.SaveSystem
{
    public static class SaveManager
    {
        /// <summary>
        /// Loads a SaveObject using the specified SaveWriter.
        /// </summary>
        /// <param name="identifier">The unique name by wich to identify the SaveObject.</param>
        /// <param name="saveObject">The SaveObject data to save.</param>
        /// <param name="writer">The SaveWriter used for saving.</param>
        /// <returns>True when the object could be saved, false otherwise.</returns>
        public static bool SaveObject(string identifier, SaveObject saveObject, SaveWriter writer)
        {
            return writer.Save(identifier, saveObject);
        }

        /// <summary>
        /// Loads a SaveObject using the specified SaveWriter.
        /// </summary>
        /// <typeparam name="T">The Type of the SaveObject.</typeparam>
        /// <param name="identifier">The unique name by wich to identify the SaveObject.</param>
        /// <param name="writer">The SaveWriter used to load the SaveObject.</param>
        /// <returns>The loaded object, null if the object could not be loaded.</returns>
        public static T LoadObject<T>(string identifier, SaveWriter writer) where T : SaveObject, new()
        {
            T saveObject = null;
            writer.Load(identifier, out saveObject);
            return saveObject;
        }

        /// <summary>
        /// Renames a SaveObject using the specified SaveWriter.
        /// </summary>
        /// <typeparam name="T">The Type of the SaveObject.</typeparam>
        /// <param name="identifier">The unique name by wich to identify the SaveObject.</param>
        /// <param name="newIdentifier">The new name by wich to identify the SaveObject.</param>
        /// <param name="writer">The SaveWriter used to rename the SaveObject.</param>
        /// <returns>True when the object could be renamed, false otherwise.</returns>
        public static bool RenameObject(string identifier, string newIdentifier, SaveWriter writer)
        {
            return writer.Rename(identifier, newIdentifier);
        }

        /// <summary>
        /// Loads a SaveObject using the specified SaveWriter.
        /// </summary>
        /// <typeparam name="T">The Type of the SaveObject.</typeparam>
        /// <param name="identifier">The unique name by wich to identify the SaveObject.</param>
        /// <param name="writer">The SaveWriter used to load the SaveObject.</param>
        /// <returns>True when the object could be deleted, false otherwise.</returns>
        public static bool DeleteObject(string identifier, SaveWriter writer)
        {
            return writer.Delete(identifier);
        }

        /// <summary>
        /// Finds all SaveObjects maching the predicate.
        /// </summary>
        /// <param name="match">The predicate used to match the SaveObject.</param>
        /// <param name="writer">The SaveWriter used to locate the SaveObject.</param>
        /// <returns>An array of matched SaveObject identifiers.</returns>
        public static string[] FindObject(Predicate<string> match, SaveWriter writer)
        {
            return writer.Find(match);
        }
    }
}
