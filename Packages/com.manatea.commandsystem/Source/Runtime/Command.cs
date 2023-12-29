using System;

namespace Manatea.CommandSystem
{
    /// <summary>
    /// Tags a static Method as a command to be fetched by the CommandManager for runtime use.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        /// <summary> The name under which this command should be accessible. </summary>
        public string Alias;
        /// <summary> The discription to be displayed alongside this command. </summary>
        public string Description;
        /// <summary> An array describing the function names used for autocompletion. </summary>
        public string[] ParameterAutocompleteDelegates;

        /// <param name="alias"> The name under which this command should be accessible. </param>
        /// <param name="description"> The discription to be displayed alongside this command. </param>
        /// <param name="parameterAutocompleteDelegates"> 
        /// An array describing the function names used for autocompletion.
        /// Each array id maps to the corresponding parameter id of the target function and describes the name of the autocompletion function to look for.
        /// Autocompletion functions must be static, return a string[] containing all available autocompletion options and be contained in the same scope as the command function itself.
        /// </param>
        public CommandAttribute(string alias = "", string description = "", string[] parameterAutocompleteDelegates = null)
        {
            Alias = alias;
            Description = description;
            ParameterAutocompleteDelegates = parameterAutocompleteDelegates;
        }
    }
}
