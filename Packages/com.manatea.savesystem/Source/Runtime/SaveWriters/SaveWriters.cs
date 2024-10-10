using System;

namespace Manatea.SaveSystem
{
    public abstract class SaveWriter
    {
        /// <summary>
        /// Describes an abstract way of saving a SaveObject to disc.
        /// </summary>
        /// <param name="identifier">The unique name by wich to identify the SaveObject.</param>
        /// <param name="saveObject">The SaveObject data to save.</param>
        /// <returns>True when the save operation completed without errors, false otherwise.</returns>
        public abstract bool Save(string identifier, SaveObject saveObject);

        /// <summary>
        /// Describes an abstract way of loading a SaveObject from disc.
        /// </summary>
        /// <typeparam name="T">The Type of the SaveObject.</typeparam>
        /// <param name="identifier">The unique name by wich to identify the SaveObject.</param>
        /// <param name="saveObject">The SaveObject to be loaded.</param>
        /// <returns>True when the load operation completed without errors, false otherwise.</returns>
        public abstract bool Load<T>(string identifier, out T saveObject) where T : SaveObject, new();

        /// <summary>
        /// Describes an abstract way of renaming a SaveObject on disc.
        /// </summary>
        /// <param name="identifier">The unique name by wich to identify the SaveObject.</param>
        /// <param name="newIdentifier">The new name by wich to identify the SaveObject.</param>
        /// <returns>True when the rename operation completed without errors, false otherwise.</returns>
        public abstract bool Rename(string identifier, string newIdentifier);

        /// <summary>
        /// Describes an abstract way of deleting a SaveObject from disc.
        /// </summary>
        /// <param name="identifier">The unique name by wich to identify the SaveObject.</param>
        /// <returns>True when the delete operation completed without errors, false otherwise.</returns>
        public abstract bool Delete(string identifier);

        /// <summary>
        /// Describes an abstract way of finding a list of valid SaveObject identifiers.
        /// </summary>
        /// <param name="match">The predicate to define matching SaveObject identifiers.</param>
        /// <returns>A list of all matching SaveObject identifiers.</returns>
        public abstract string[] Find(Predicate<string> match);
    }
}
