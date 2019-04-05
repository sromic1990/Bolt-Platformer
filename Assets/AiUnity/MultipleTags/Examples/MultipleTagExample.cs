// ***********************************************************************
// Assembly   : Assembly-CSharp
// Company    : AiUnity
// Author     : AiDesigner
//
// Created    : 07-07-2017
// Modified   : 02-07-2018
// ***********************************************************************
using AiUnity.Common.Extensions;
using AiUnity.Common.Log;
using AiUnity.MultipleTags.Core;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace AiUnity.MultipleTags.Examples
{
    /// <summary>
    /// Class MultipleTagExample.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    public class MultipleTagExample : MonoBehaviour
    {
        #region Fields
        private GameObject GoRedBlue;
        private GameObject GoT1;
        private GameObject GoT1T2;
        private GameObject GoT2T3;
        private GameObject GoT3Red;
        #endregion

        public TagLogic MyLogic;
        public List<string> MyTags;

        #region Properties
        /// <summary> Gets the tag service. </summary>
        private static TagService TagService { get { return TagService.Instance; } }

        /// <summary> The game console exist in the build and can be used to log messages.</summary>
        private IGameConsoleController GameConsoleController { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Awakes this instance.
        /// </summary>
        private void Awake()
        {
            // Get gameObject handles to confirm example results
            this.GoT1 = GameObject.Find("GoT1");
            this.GoT1T2 = GameObject.Find("GoT1T2");
            this.GoT2T3 = GameObject.Find("GoT2T3");
            this.GoT3Red = GameObject.Find("GoT3Red");
            this.GoRedBlue = GameObject.Find("GoRedBlue");
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        void Start()
        {
            GameConsoleController = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<IGameConsoleController>()).FirstOrDefault();
            if (GameConsoleController != null)
            {
                GameConsoleController.SetIconEnable(false);
                GameConsoleController.SetConsoleActive(true);
                GameConsoleController.SetFontSize(8);
                GameConsoleController.SetLogLevelFilter(LogLevels.Everything);
            }

            //*******************************************************************************
            // These examples search for gameObjects by tag(s)
            //*******************************************************************************
            LogFork(string.Format("{1}<b>{0}  Search for gameObjects by tag(s)  {0}</b>", new string('*', 5), Environment.NewLine));

            // Find gameObjects with tag "T1".
            GameObject[] gameObjects = TagService.FindGameObjectsWithTags("T1");
            AnalyzeResults("T1", gameObjects, this.GoT1, this.GoT1T2);

            // Find gameObjects with tag "T1" in a type safe manner.
            gameObjects = TagService.FindGameObjectsWithTags(TagAccessExample.T1);
            AnalyzeResults("T1", gameObjects, this.GoT1, this.GoT1T2);

            // Find gameObjects with tags "T1" or "T3" in a type safe manner.
            gameObjects = TagService.FindGameObjectsWithTags(TagLogic.Or, TagAccessExample.T1, TagAccessExample.T3);
            AnalyzeResults("T1 | T3", gameObjects, this.GoT1, this.GoT1T2, this.GoT2T3, this.GoT3Red);

            // Find gameObjects with tags "T1" or "T3", and is Color.Red.
            gameObjects = TagService.FindGameObjectsWithTags("(T1 | T3) & Color.Red");
            AnalyzeResults("(T1 | T3) & Color.Red", gameObjects, this.GoT3Red);

            // Use regex pattern to find gameObjects with tags starting with "T"
            gameObjects = TagService.FindGameObjectsWithTags("T.*");
            AnalyzeResults("T.*", gameObjects, this.GoT1, this.GoT1T2, this.GoT2T3, this.GoT3Red);

            // Use extension method to refine search to require tag "T3"
            gameObjects = gameObjects.FindGameObjectsWithTags(TagLogic.And, TagAccessExample.T3);
            AnalyzeResults("T.* & T3(Extension method)", gameObjects, this.GoT2T3, this.GoT3Red);

            //*******************************************************************************
            // These examples search for gameObjects by tag group(s)
            //*******************************************************************************
            LogFork(string.Format("{1}<b>{0}  Search for gameObjects by tag group(s)  {0}</b>", new string('*', 5), Environment.NewLine));

            // Find gameObjects with tags "Color.Blue" in a type safe manner.
            gameObjects = TagService.FindGameObjectsWithTags(TagAccessExample.Color.Blue);
            AnalyzeResults("Color.Blue", gameObjects, this.GoRedBlue);

            // Find gameObjects that use any tag in group "Color".
            gameObjects = TagService.FindGameObjectsWithTags(TagAccessExample.Color.Any());
            AnalyzeResults("Color", gameObjects, this.GoT3Red, this.GoRedBlue);

            //*******************************************************************************
            // These examples check if a gameObject has a specified tag
            // Note that a tagPath must already exist for it to be assigned at runtime.
            //*******************************************************************************
            LogFork(string.Format("{1}<b>{0}  Check if a gameObject has a specified tag(s)  {0}</b>", new string('*', 5), Environment.NewLine));

            // Check if gameObject has tag "T1"
            bool hasTag = this.GoT1.HasTag(TagAccessExample.T2);
            AnalyzeResults("T2", this.GoT1, hasTag, false);

            // Check if gameObject has tag "T1" or "T2"
            hasTag = this.GoT1.HasTags(TagLogic.Or, TagAccessExample.T1, TagAccessExample.T2);
            AnalyzeResults("T1 | T2", this.GoT1, hasTag, true);

            //*******************************************************************************
            // These examples modify a gameObject tag at runtime.
            // The exact TagPath (i.e. T1/T2) must exist in Unity to be used at runtime.
            // Note that modified gameObject tag reverts when play mode exists.
            //*******************************************************************************
            LogFork(string.Format("{1}<b>{0}  Modify a gameObject tag at runtime  {0}</b>", new string('*', 5), Environment.NewLine));

            // Add tag to gameObject
            gameObject.AddTags(TagAccessExample.T2);
            AnalyzeResults("AddTags", gameObject, "T2", "T1/T2");

            // Remove tag from gameObject
            gameObject.RemoveTags(TagAccessExample.T1);
            AnalyzeResults("RemoveTags", gameObject, "T1", "T2");
        }

        // Analyze results of searching for gameObjects by tag(s)
        /// <summary>
        /// Analyzes the results.
        /// </summary>
        /// <param name="search">The search.</param>
        /// <param name="gameObjects">The game objects.</param>
        /// <param name="expectGameObjects">The expect game objects.</param>
        void AnalyzeResults(string search, GameObject[] gameObjects, params GameObject[] expectGameObjects)
        {
            if (gameObjects.ScrambledEquals(expectGameObjects))
            {
                LogFork(string.Format("<color=green><b>PASS</b></color>: Tag search={0} found GameObject(s)={1}", search, string.Join(", ", gameObjects.Select(g => g.name).ToArray())));
            }
            else
            {
                LogFork(string.Format("<color=red><b>FAIL</b></color>: Tag search={0} found GameObject(s)={1} (expected GameObject(s)={2})", search,
                    string.Join(",", gameObjects.Select(g => g.name).ToArray()), string.Join(", ", expectGameObjects.Select(g => g.name).ToArray())));
            }
        }

        // Analyze results of checking if a gameObject has a specified tag
        /// <summary>
        /// Analyzes the results.
        /// </summary>
        /// <param name="search">The search.</param>
        /// <param name="gameObject">The game object.</param>
        /// <param name="hasTag">if set to <c>true</c> [has tag].</param>
        /// <param name="expectHas">if set to <c>true</c> [expect has].</param>
        void AnalyzeResults(string search, GameObject gameObject, bool hasTag, bool expectHas)
        {
            if (hasTag == expectHas)
            {
                LogFork(string.Format("<color=green><b>PASS</b></color>: GameObjects={0} has tags {1} is {2}", gameObject.name, search, hasTag));
            }
            else
            {
                LogFork(string.Format("<color=red><b>FAIL</b></color>: GameObjects={0} has tags {1} is {2} (expected = {3})", gameObject.name, search, hasTag, expectHas));
            }
        }

        // Analyze results of modifying a gameObject tag at runtime
        /// <summary>
        /// Analyzes the results.
        /// </summary>
        /// <param name="operation">The operation.</param>
        /// <param name="gameObject">The game object.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="expectedTag">The expected tag.</param>
        void AnalyzeResults(string operation, GameObject gameObject, string tag, string expectedTag)
        {
            if (gameObject.tag == expectedTag)
            {
                LogFork(string.Format("<color=green><b>PASS</b></color>: {0}({1}) on GameObject={2} resulting in tag={3}", operation, tag, gameObject.name, gameObject.tag));
            }
            else
            {
                LogFork(string.Format("<color=red><b>FAIL</b></color>: {0}({1}) on GameObject={2} resulting in tag={3} (expected={4})", operation, tag, gameObject.name, gameObject.tag, expectedTag));
            }
        }

        /// <summary>
        /// Logs the fork.
        /// </summary>
        /// <param name="message">The message.</param>
        void LogFork(string message)
        {
            Debug.Log(message);

            // The game console is used to display results at runtime or in a built game.
            string gameConsoleMessage = message + Environment.NewLine;
            GameConsoleController.AddMessage((int)LogLevels.Debug, gameConsoleMessage, GetType().Name, DateTime.Now);
        }
        #endregion
    }
}