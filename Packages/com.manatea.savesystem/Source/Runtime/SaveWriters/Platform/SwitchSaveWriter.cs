//#if UNITY_SWITCH
//
//using System;
//using System.ComponentModel;
//using System.Globalization;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.IO;
//using UnityEngine;
//using nn.fs;
//
//namespace Manatea.SaveSystem
//{
//    public class SwitchSaveWriter : SaveWriter          // TODO extend FileSaveWriter instead of SaveWriter
//    {
//        public virtual bool UsePrettyPrint => Debug.isDebugBuild;
//
//        public virtual string DirectoryPath => Application.persistentDataPath + "/Saves/";
//        public virtual string Extension => "";
//
//
//        private static nn.account.Uid userId;
//        private const string mountName = "AdventureRoots";
//        private const string fileName = "MySaveData";
//
//        public static string filePath;
//
//        private static nn.fs.FileHandle fileHandle = new nn.fs.FileHandle();
//
//        public static bool initialized;
//
//        public static void Init()
//        {
//            Debug.Log("Account.Initialize");
//            nn.account.Account.Initialize();
//            Debug.Log("Get UserHandle");
//            nn.account.UserHandle userHandle = new nn.account.UserHandle();
//
//            // Open the user that was selected before the application started.
//            // This assumes that Startup user account is set to Required.
//            Debug.Log("TryOpenPreselectedUser");
//            if (nn.account.Account.TryOpenPreselectedUser(ref userHandle))
//            {
//                // Get the user ID of the preselected user account.
//                Debug.Log("Account.GetUserId");
//                nn.Result rresult = nn.account.Account.GetUserId(ref userId, userHandle);
//                if (rresult.IsSuccess())
//                {
//                    Debug.LogFormat("Loaded User {0}", userId);
//                }
//                else
//                {
//                    Debug.LogErrorFormat("Failed loading user {0}", userId);
//                }
//            }
//            else
//            {
//                Debug.LogErrorFormat("Failed loading user {0}", userId);
//            }
//
//
//            // Mount the save data archive as "save" for the selected user account.
//            Debug.Log("Mounting save data archive");
//            nn.Result result = nn.fs.SaveData.Mount(mountName, userId);
//
//            if (result.IsSuccess() == false)
//            {
//                Debug.Log("Critical Error: File System could not be mounted.");
//                result.abortUnlessSuccess();
//                initialized = false;
//            }
//            else
//            {
//                Debug.LogFormat("Save Manager Initialized. Mounted {0} User {1}", mountName, userId);
//                initialized = true;
//            }
//        }
//
//        public bool SaveOld(string identifier, SaveObject saveObject)
//        {
//            try
//            {
//                string json = JsonUtility.ToJson(saveObject, UsePrettyPrint);
//
//
//                return true;
//            }
//            catch (Exception e)
//            {
//                Debug.LogException(e.InnerException);
//                return false;
//            }
//        }
//
//        public override bool Load<T>(string identifier, out T saveObject)
//        {
//            if (!initialized)
//            {
//                OpenConsoleUtility.logList.Add("Init Switch save system");
//                Debug.Log("Init Switch save system");
//                Init();
//            }
//
//            saveObject = null;
//            OpenConsoleUtility.logList.Add("Not loaded");
//
//            try
//            {
//
//                Debug.Log("Try to load switch SaveFile");
//                OpenConsoleUtility.logList.Add("Try to load switch SaveFile");
//#if UNITY_SWITCH// && !UNITY_EDITOR
//
//                // The NintendoSDK plug-in uses a FileHandle object for file operations.
//                Debug.Log("Get FileHandle");
//                nn.fs.FileHandle handle = new nn.fs.FileHandle();
//
//
//                Debug.Log("Open File");
//                OpenConsoleUtility.logList.Add("Open File");
//                filePath = string.Format("{0}:/{1}", mountName, fileName);
//                nn.Result result = nn.fs.File.Open(ref handle, filePath, nn.fs.OpenFileMode.Read);
//                Debug.Log("After Open File");
//                if (!result.IsSuccess())
//                {
//                    if (nn.fs.FileSystem.ResultPathNotFound.Includes(result))
//                    {
//                        OpenConsoleUtility.logList.Add("File not found");
//                        Debug.LogFormat("File not found: {0}", filePath);
//                        return false;
//                    }
//                    else
//                    {
//                        OpenConsoleUtility.logList.Add("Unable to open");
//                        Debug.LogErrorFormat("Unable to open {0}: {1}", filePath, result.ToString());
//                        return false;
//                    }
//                }
//                Debug.Log("After result.IsSuccess");
//
//
//                // Get the file size.
//                long fileSize = 0;
//                nn.fs.File.GetSize(ref fileSize, handle);
//                // Allocate a buffer that matches the file size.
//                byte[] bytedata = new byte[fileSize];
//                // Read the save data into the buffer.
//                nn.fs.File.Read(handle, 0, bytedata, fileSize);
//                Debug.Log("Read(handle, 0, bytedata, fileSize");
//                // Close the file.
//                nn.fs.File.Close(handle);
//                Debug.Log("File.Close(handle");
//
//                // Make data
//                Debug.Log("Pre MemoryStream stream");
//                using (MemoryStream stream = new MemoryStream(bytedata))
//                {
//                    Debug.Log("Pre StreamReader");
//                    var streamReader = new StreamReader(stream, Encoding.UTF8);
//                    Debug.Log("Pre ReadToEnd");
//                    string json = streamReader.ReadToEnd();
//                    Debug.Log("Pre json");
//                    OpenConsoleUtility.savedData = json;
//                }
//                Debug.Log("After MemoryStream");
//
//#endif
//
//                return true;
//            }
//            catch (Exception e)
//            {
//                Debug.LogException(e.InnerException);
//                return false;
//            }
//        }
//
//        protected virtual string LoadFile(string path)
//        {
//            // TODO read file
//
//            return "";
//            //return File.ReadAllText(path);
//        }
//        protected virtual string BuildFilePath(string identifier)
//        {
//            string.Format("{0}:/{1}", mountName, fileName);
//            return DirectoryPath + identifier + Extension;
//        }
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//        public override bool Save(string identifier, SaveObject saveObject)
//        {
//            Debug.Log("Try to save switch SaveFile");
//#if UNITY_SWITCH// && !UNITY_EDITOR
//
//            if (!initialized)
//            {
//                OpenConsoleUtility.logList.Add("Init Switch save system");
//                Debug.Log("Init Switch save system");
//                Init();
//            }
//
//            // This is it!
//            string json;
//            byte[] dataByteArray;
//            using (MemoryStream stream = new MemoryStream())
//            {
//                var streamWriter = new StreamWriter(stream, Encoding.UTF8);
//                try
//                {
//                    json = JsonUtility.ToJson(saveObject, UsePrettyPrint);
//
//                    streamWriter.Write(json);
//                    streamWriter.Flush();//otherwise you are risking empty stream
//                    stream.Seek(0, SeekOrigin.Begin);
//
//                    // Test and work with the stream here. 
//                    // If you need to start back at the beginning, be sure to Seek again.
//                }
//                finally
//                {
//                    streamWriter.Dispose();
//                    stream.Close();
//                    dataByteArray = stream.GetBuffer();
//
//                    //Debug.Assert(dataByteArray.LongLength == journalSaveDataSize);
//                }
//            }
//            OpenConsoleUtility.logList.Add("Wrote Data Stream");
//            Debug.Log("Wrote Data Stream");
//            Debug.Log(json);
//
//
//
//            // This method prevents the user from quitting the game while saving.
//            // This is required for Nintendo Switch Guideline 0080
//            // This method must be called on the main thread.
//            Debug.Log("EnterExitRequestHandlingSection");
//            OpenConsoleUtility.logList.Add("EnterExitRequestHandlingSection");
//            //UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();
//
//            // The NintendoSDK plug-in uses a FileHandle object for file operations.
//            Debug.Log("Get FileHandle");
//            OpenConsoleUtility.logList.Add("Get FileHandle");
//            nn.fs.FileHandle handle = new nn.fs.FileHandle();
//
//            filePath = string.Format("{0}:/{1}", mountName, fileName);
//            while (true)
//            {
//                // Attempt to open the file in write mode.
//                OpenConsoleUtility.logList.Add("File.Open");
//                Debug.Log("File.Open");
//                nn.Result rresult = nn.fs.File.Open(ref handle, filePath, nn.fs.OpenFileMode.Write);
//                // Check if file was opened successfully.
//                if (rresult.IsSuccess())
//                {
//                    OpenConsoleUtility.logList.Add("File { 0} is open and ready to write");
//                    Debug.LogFormat("File {0} is open and ready to write", filePath);
//                    // Exit the loop because the file was successfully opened.
//                    break;
//                }
//                else
//                {
//                    Debug.Log("ResultPathNotFound.Includes");
//                    OpenConsoleUtility.logList.Add("ResultPathNotFound");
//                    if (nn.fs.FileSystem.ResultPathNotFound.Includes(rresult)) {
//                        // Create a file with the size of the encoded data if no entry exists.
//                        rresult = nn.fs.File.Create(filePath, dataByteArray.LongLength);
//                        Debug.Log("File.Create");
//                        OpenConsoleUtility.logList.Add("File.Create");
//                        // Check if the file was successfully created.
//                        if (!rresult.IsSuccess())
//                        {
//                            OpenConsoleUtility.logList.Add("Failed to create {0}: {1} - Length");
//                            Debug.LogErrorFormat("Failed to create {0}: {1} - Length {2}", filePath, rresult.ToString(),dataByteArray.LongLength);
//                            return false;
//                        }
//                        else
//                        {
//                            Debug.LogFormat("Created Save File {0}: {1}", filePath, rresult.ToString());
//                        }
//                    }
//                    else
//                    {
//                        // Generic fallback error handling for debugging purposes.
//                        Debug.LogErrorFormat("Failed to open {0}: {1}", filePath, rresult.ToString());
//                        return false;
//                    }
//                }
//            }
//
//            // Set the file to the size of the binary data.
//            Debug.Log("File.SetSize");
//            OpenConsoleUtility.logList.Add("File.SetSize");
//            nn.Result result = nn.fs.File.SetSize(handle, dataByteArray.LongLength);
//
//            // You do not need to handle this error if you are sure there will be enough space.
//            Debug.Log("FileSystem.ResultUsableSpaceNotEnough.Includes");
//            OpenConsoleUtility.logList.Add("FileSystem.ResultUsableSpaceNotEnough.Includes");
//            if (nn.fs.FileSystem.ResultUsableSpaceNotEnough.Includes(result))
//            {
//                OpenConsoleUtility.logList.Add("Insufficient space to write {0} bytes to");
//                Debug.LogErrorFormat("Insufficient space to write {0} bytes to {1}", dataByteArray.LongLength, filePath);
//                return false;
//            }
//
//            // NOTE: Calling File.Write() with WriteOption.Flush incurs two write operations.
//            Debug.Log("File.Write");
//            OpenConsoleUtility.logList.Add("File.Write");
//            result = nn.fs.File.Write(handle, 0, dataByteArray, dataByteArray.LongLength, nn.fs.WriteOption.Flush);
//
//            // You do not need to handle this error if you are sure there will be enough space.
//            // if (nn.fs.ResultUsableSpaceNotEnough.Includes(result))
//            // {
//            //     Debug.LogErrorFormat("Insufficient space to write {0} bytes to {1}", dataByteArray.LongLength, filePath);
//            // }
//
//            // The file must be closed before committing.
//            Debug.Log("File.Close");
//            OpenConsoleUtility.logList.Add("File.Close");
//            nn.fs.File.Close(handle);
//
//            // Verify that the write operation was successful before committing.
//            if (!result.IsSuccess())
//            {
//                OpenConsoleUtility.logList.Add("Failed to write");
//                Debug.LogErrorFormat("Failed to write {0}: {1}", filePath, result.ToString());
//                return false;
//            }
//
//            // This method moves the data from the journaling area to the main storage area.
//            // If you do not call this method, all changes will be lost when the application closes.
//            // Only call this when you are sure that all previous operations succeeded.
//            Debug.Log("FileSystem.Commit");
//            OpenConsoleUtility.logList.Add("FileSystem.Commit");
//            nn.fs.FileSystem.Commit(mountName);
//
//            // End preventing the user from quitting the game while saving. 
//            // This is required for Nintendo Switch Guideline 0080
//            Debug.Log("Notification.LeaveExitRequestHandlingSection");
//            OpenConsoleUtility.logList.Add("Notification.LeaveExitRequestHandlingSection");
//            //UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
//
//#endif
//
//            return true;
//        }
//    }
//}
//
//#endif