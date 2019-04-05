using UnityEngine;
using AiUnity.Common.IO;
using AiUnity.Common.Patterns;
using System.IO;
using System.Linq;
using AiUnity.MultipleTags.Common;
using AiUnity.Common.InternalLog;

namespace AiUnity.MultipleTags.Core
{
    public class TagAccessFileInfo : UnityFileInfo<TagAccessFileInfo>
    {
        // Internal logger singleton
        private static IInternalLogger Logger { get { return MultipleTagsInternalLogger.Instance; } }

        //private TagAccessFileInfo()
        public TagAccessFileInfo()
        {
            string configFullFileName = PlayerPrefs.GetString("AiUnityTagAccessFullFileName");

            if (string.IsNullOrEmpty(configFullFileName))
            {
                string CLoggerFile = Directory.GetFiles(Application.dataPath, "TagService.cs", SearchOption.AllDirectories).
                    Select(s => s.Replace('\\', '/')).FirstOrDefault(s => s.Contains(@"/MultipleTags/Core/"));
                string aiUnityPath = string.IsNullOrEmpty(CLoggerFile) ? Application.dataPath : CLoggerFile.Substring(0, CLoggerFile.IndexOf("/MultipleTags/Core/"));
                string configPath = aiUnityPath + @"/UserData/MultipleTags";

                Directory.CreateDirectory(configPath);

                configFullFileName = configPath + "/TagAccess.cs";
            }
            FileInfo = new FileInfo(configFullFileName);
        }

        public void SetConfigFileName(string configFullFileName)
        {
            PlayerPrefs.SetString("AiUnityTagAccessFullFileName", configFullFileName);
            FileInfo = new FileInfo(configFullFileName);
        }

        /// <summary>
        /// Saves to file.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="file">The file.</param>
        public void Save(string code)
        {
            Logger.Info("Saving TagAccess file = {0}", FileInfo.FullName);
            using (var writer = new StreamWriter(FileInfo.FullName, false))
            {
                try
                {
                    writer.WriteLine(code);
                    return;
                }
                catch
                {
                    Logger.Error("Failed to write code to script (file={0} code={1})", FileInfo.FullName, code);
                    throw;
                }
            }
        }

    }
}