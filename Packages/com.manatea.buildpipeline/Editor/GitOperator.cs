﻿// https://github.com/CdecPGL/unity-git-version/blob/master/Assets/PlanetaGameLabo/UnityGitVersion/Editor/GitOperator.cs

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Manatea.BuildPipeline.Git
{
    /// <summary>
    /// A class to operate git and get information.
    /// </summary>
    public static class GitOperator
    {
        /// <summary>
        /// Check if git is installed and available.
        /// </summary>
        /// <returns>True if git is available.</returns>
        public static bool CheckIfGitIsAvailable()
        {
            try
            {
                return ExecuteCommand("--version").exitCode != 0;
            }
            catch (CommandExecutionErrorException e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        /// <summary>
        /// Check if there are any changes in the repository from the last commit.
        /// </summary>
        /// <returns>True if there are changes.</returns>
        public static bool CheckIfRepositoryIsChangedFromLastCommit()
        {
            try
            {
                var result = ExecuteGitCommand("status --short");
                return !string.IsNullOrWhiteSpace(result.Replace("\n", string.Empty));
            }
            catch (GitCommandExecutionError e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the build version from git based on the most recent matching tag and
        /// commit history. This returns the version as: {major.minor.build} where 'build'
        /// represents the nth commit after the tagged commit.
        /// Note: The initial 'v' and the commit hash code are removed.
        /// </summary>
        public static string GetBuildVersion()
        {
            var version = ExecuteGitCommand(@"describe --tags --long --match ""v[0-9]*""");
            // Remove initial 'v' and ending git commit hash.
            version = version.Replace('-', '.');
            version = version.Substring(1, version.LastIndexOf('.') - 1);
            return version;
        }

        /// <summary>
        /// Get current tag of the specified commit.
        /// </summary>
        /// <param name="commitId">An id of the target commit</param>
        /// <returns>Tag. If there are no tags for the commit ID or git is not available, this function returns empty string.</returns>
        public static string GetTagFromCommitId(string commitId)
        {
            try
            {
                var tag = ExecuteGitCommand("tag -l --contains " + commitId).Replace("\n", string.Empty);
                return string.IsNullOrEmpty(tag) ? "" : tag;
            }
            catch (GitCommandExecutionError e)
            {
                Debug.LogException(e);
                return "";
            }
        }

        /// <summary>
        /// Get current a last commit id of the current branch.
        /// </summary>
        /// <param name="shortVersion">Returns short commit id if this is true.</param>
        /// <returns>Commit ID. If there are no commits or git is not available, this function returns empty string.</returns>
        public static string GetLastCommitId(bool shortVersion)
        {
            try
            {
                var commitId = ExecuteGitCommand($"rev-parse{(shortVersion ? " --short" : "")} HEAD")
                    .Replace("\n", string.Empty);
                if (!string.IsNullOrEmpty(commitId))
                {
                    return commitId;
                }

                Debug.LogError(
                    "Failed to get commit id. Check if git is installed and the directory of this project is initialized as a git repository.");
                return "";
            }
            catch (GitCommandExecutionError e)
            {
                Debug.LogException(e);
                return "";
            }
        }

        /// <summary>
        /// Get a hash of the difference between current worktree and the last commit.
        /// </summary>
        /// <param name="shortVersion">Returns short hash with 7 characters if this is true.</param>
        /// <returns>A SHA1 hash of diff between current repository and last commit</returns>
        public static string GetHashOfChangesFromLastCommit(bool shortVersion)
        {
            try
            {
                // Get a string representing difference between worktree and the last commit.
                var diff = ExecuteGitCommand("diff HEAD");

                // Add file names and last update datetime of untracked file to difference string because untracked files (newly added files) are not included in the result of "git diff" command.
                var changeResult = ExecuteGitCommand("status -s -uall --porcelain");
                var changes = changeResult.Split('\n');
                var untrackedFilePaths = changes.Where(c => c.StartsWith("??")).Select(c => c.TrimStart('?', ' '));
                // To ensure consistency among machines with different locales, we use UTC as timezone and ISO datetime string. 
                var untrackedFilePathsWithUpdateTimes = untrackedFilePaths.Select(u =>
                    $"{u}@{new FileInfo(u).LastWriteTimeUtc:yyyy-MM-dd'T'HH:mm:ss'Z'}");

                diff += string.Join("\n", untrackedFilePathsWithUpdateTimes);

                // Generate SHA1 hash, which is used in Git
                var hash = GetHashString<SHA1CryptoServiceProvider>(diff);
                // Make hash length 7 which is same as the length of short hash in Git if short flag is enabled
                return shortVersion ? hash.Substring(0, 7) : hash;
            }
            catch (GitCommandExecutionError e)
            {
                Debug.LogException(e);
                return "";
            }
        }

        /// <summary>
        /// Get a result of git describe
        /// </summary>
        /// <returns>A result of git describe</returns>
        public static string GetDescription(bool enableLightweightTagMatch)
        {
            try
            {
                return ExecuteGitCommand(enableLightweightTagMatch ? "describe --tags" : "describe")
                    .Replace("\n", string.Empty);
            }
            catch (GitCommandExecutionError e)
            {
                Debug.LogException(e);
                return "";
            }
        }

        private static string ExecuteGitCommand(string arguments, float timeOutSeconds = 10)
        {
            try
            {
                // Use --no-pager option to avoid to wait for user input by pagination.
                var (standardOutput, standardError, exitCode) =
                    ExecuteCommand($"git --no-pager {arguments}", timeOutSeconds);
                if (exitCode != 0)
                {
                    throw new GitCommandExecutionError(arguments, exitCode, standardError);
                }

                return standardOutput;
            }
            catch (CommandExecutionErrorException e)
            {
                throw new GitCommandExecutionError(arguments, e.Message);
            }
        }

        private static (string standardOutput, string standardError, int exitCode) ExecuteCommand(string command,
            float timeOutSeconds = 10)
        {
            // Create a Process object
            using (var process = new System.Diagnostics.Process())
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                        // Get the path of ComSpec(cmd.exe) and set it to FileName property
                        var cmdPath = Environment.GetEnvironmentVariable("ComSpec");
                        if (cmdPath == null)
                        {
                            throw new CommandExecutionErrorException(command,
                                "Command Prompt is not found because environment variable \"ComSpec\" doesn'T exist.");
                        }

                        process.StartInfo.FileName = cmdPath;
                        process.StartInfo.Arguments = "/c " + command;
                        break;
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.LinuxEditor:
                        process.StartInfo.FileName = "/bin/bash";
                        process.StartInfo.Arguments = "-c \" " + command + "\"";
                        break;
                    default:
                        {
                            throw new CommandExecutionErrorException(command,
                                $"Command execution is not supported in current platform ({Application.platform}).");
                        }
                }

                // Enable to read outputs
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = false;
                // Prevent to show window
                process.StartInfo.CreateNoWindow = true;

                // Run process
                process.Start();

                // Read output
                // To prevent for this process to be blocked by full of output stream buffer, we wait finish of the command with reading output stream asynchronously.
                using (var standardOutputTask = Task.Run(async () => await process.StandardOutput.ReadToEndAsync()))
                using (var standardErrorTask = Task.Run(async () => await process.StandardError.ReadToEndAsync()))
                {
                    // Wait for process finish
                    if (!process.WaitForExit((int)(timeOutSeconds * 1000)))
                    {
                        // Stop the process if timeout
                        process.Kill();
                        process.WaitForExit();
                        standardOutputTask.Wait();
                        standardErrorTask.Wait();
                        throw new CommandExecutionErrorException(command, "Timeout");
                    }

                    standardOutputTask.Wait();
                    standardErrorTask.Wait();
                    return (standardOutputTask.Result.Replace("\r\n", "\n"),
                        standardErrorTask.Result.Replace("\r\n", "\n"), process.ExitCode);
                }
            }
        }

        private static string GetHashString<T>(string text) where T : HashAlgorithm, new()
        {
            var data = Encoding.UTF8.GetBytes(text);
            using (var algorithm = new T())
            {
                var hashBytes = algorithm.ComputeHash(data);
                // Convert byte array to hexadecimal string
                var result = new StringBuilder();
                foreach (var hashByte in hashBytes)
                {
                    result.Append(hashByte.ToString("x2"));
                }

                return result.ToString();
            }
        }
    }

    /// <summary>
    /// An exception class for git command execution error.
    /// </summary>
    public sealed class GitCommandExecutionError : Exception
    {
        public GitCommandExecutionError(string arguments, int exitCode, string standardError) : base(
            $"Failed to execute git command with arguments \"{arguments}\" and exit code \"{exitCode}\". \"{standardError}\"")
        {
        }

        public GitCommandExecutionError(string arguments, string standardError) : base(
            $"Failed to execute git command with arguments \"{arguments}\". \"{standardError}\"")
        {
        }
    }

    internal sealed class CommandExecutionErrorException : Exception
    {
        public CommandExecutionErrorException(string command, string reason) : base(
            $"Failed to execute command \"{command}\" due to \"{reason}\"")
        {
        }
    }
}
