// ***********************************************************************
// Assembly   : Assembly-CSharp-Editor
// Company    : AiUnity
// Author     : AiDesigner
//
// Created    : 07-19-2017
// Modified   : 08-29-2017
// ***********************************************************************
using AiUnity.Common.InternalLog;
using AiUnity.MultipleTags.Common;
using System;
using UnityEditor;
using UnityEngine;

namespace AiUnity.MultipleTags.Editor
{
    /// <summary>
    /// Updates TagAccess when a change to Unity tags is detected.
    /// </summary>
    public class TagAccessAutoUpdate
    {
        #region Fields
        /// <summary> The update script interval </summary>
        static float updateScriptInterval = 5;
        /// <summary> The update script time </summary>
        static double updateScriptTime = 10;
        #endregion

        #region Properties
        /// <summary> Gets the tag manager. </summary>
        public static TagManager TagManager { get { return TagManager.Instance; } }

        // Internal logger singleton
        /// <summary> Gets the logger. </summary>
        private static IInternalLogger Logger { get { return MultipleTagsInternalLogger.Instance; } }
        #endregion

        #region Methods
        /// <summary>
        /// Updates the scripts setup.
        /// </summary>
        [InitializeOnLoadMethod]
        public static void UpdateScriptsSetup()
        {
            TagAccessCreator.Instance.LoadTagHash();
            EditorApplication.update += UpdateScripts;
        }

        /// <summary>
        /// Updates the scripts.
        /// </summary>
        private static void UpdateScripts()
        {
            if (EditorApplication.timeSinceStartup > updateScriptTime)
            {
                bool autoUpdate = Convert.ToBoolean(EditorPrefs.GetInt("AiUnityMultipleTagsAutoUpdate", 0));

                if (autoUpdate && TagAccessCreator.Instance.UpdateAvailable())
                {
                    Logger.Info("Auto updating TagAccess file due to tag changes.");
                    TagAccessCreator.Instance.Create();
                }
                updateScriptTime = EditorApplication.timeSinceStartup + updateScriptInterval;
            }
        }
        #endregion
    }
}