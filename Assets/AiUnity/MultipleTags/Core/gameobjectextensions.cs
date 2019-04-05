// ***********************************************************************
// Assembly   : Assembly-CSharp
// Company    : AiUnity
// Author     : AiDesigner
//
// Created    : 07-07-2017
// Modified   : 08-04-2017
// ***********************************************************************
using AiUnity.Common.InternalLog;
using AiUnity.MultipleTags.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AiUnity.MultipleTags.Core
{
    /// <summary>
    /// Class GameObjectExtensions.
    /// </summary>
    /// <tags>MultipleTagsAPI</tags>
    public static class GameObjectExtensions
    {
        #region Properties
        /// <summary>The internal debug logger.</summary>
        private static IInternalLogger Logger { get { return MultipleTagsInternalLogger.Instance; } }

        /// <summary>Gets the tag service.</summary>
        private static TagService TagService { get { return TagService.Instance; } }
        #endregion

        #region Methods
        /// <summary>
        /// Add tag(s) to gameObject.  To succeed the resulting tagPath must already exist in Unity (i.e. "T1/T2").
        /// </summary>
        /// <param name="gameObject">The gameObject.</param>
        /// <param name="tags">The tags to add.</param>
        public static void AddTags(this GameObject gameObject, params string[] tags)
        {
            gameObject.SetTags(gameObject.Tags().Concat(tags).ToArray());
        }

        /// <summary>
        /// Add tag(s) to gameObjects.  To succeed the resulting tagPath must already exist in Unity (i.e. "T1/T2").
        /// </summary>
        /// <param name="gameObjects">The gameObjects.</param>
        /// <param name="tags">The tags to add.</param>
        public static void AddTags(this IEnumerable<GameObject> gameObjects, params string[] tags)
        {
            foreach (GameObject gameObject in gameObjects)
            {
                gameObject.SetTags(gameObject.Tags().Concat(tags).ToArray());
            }
        }

        /// <summary>
        /// Find gameObjects by taking existing gameObjects tagPath and performing tagLogic operation with the derived tagPattern tagPaths.
        /// </summary>
        /// <param name="gameObjects">The starting gameObjects.</param>
        /// <param name="tagLogic">The tag logic operation between gameObjects tagPath and tagPattern tagPaths.</param>
        /// <param name="tagPattern">The tag pattern used to generate a set of tagPaths.</param>
        public static GameObject[] FindGameObjectsWithTags(this IEnumerable<GameObject> gameObjects, TagLogic tagLogic, string tagPattern)
        {
            return TagService.FindGameObjectsWithTags(TagService.GetTagPathMatches(tagLogic, gameObjects.Tags(), TagService.GetTagPathMatches(tagPattern)));
        }

        /// <summary>
        /// Find gameObjects by taking existing gameObjects tagPath and performing tagLogic operation with the derived tagPattern tagPaths.
        /// </summary>
        /// <param name="gameObject">The starting gameObject.</param>
        /// <param name="tagLogic">The tag logic operation between gameObject tagPath and tagPattern tagPaths.</param>
        /// <param name="tagPattern">The tag pattern used to generate a set of tagPaths.</param>
        public static GameObject[] FindGameObjectsWithTags(this GameObject gameObject, TagLogic tagLogic, string tagPattern)
        {
            return TagService.FindGameObjectsWithTags(TagService.GetTagPathMatches(tagLogic, TagService.GetTagPathMatches(tagPattern), TagService.FormatTagPaths(gameObject.tag)));
        }

        /// <summary>
        /// Determines if gameObject has the specified tag.
        /// </summary>
        /// <param name="gameObject">The gameObject.</param>
        /// <param name="tag">The tag.</param>
        public static bool HasTag(this GameObject gameObject, string tag)
        {
            return gameObject.Tags().Contains(tag);
        }

        /// <summary>
        /// Determines if component has the tags derived from specified tagLogic and tag(s).
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="tagLogic">The tag search.</param>
        /// <param name="tags">The tags.</param>
        public static bool HasTags(this Component component, TagLogic tagLogic, params string[] tags)
        {
            return component.gameObject.HasTags(tagLogic, tags);
        }

        /// <summary>
        /// Determines if gameObject has the tags derived from specified tagLogic and tag(s).
        /// </summary>
        /// <param name="gameObject">The gameObject.</param>
        /// <param name="tagSearch">The tag search.</param>
        /// <param name="tags">The tags.</param>
        public static bool HasTags(this GameObject gameObject, TagLogic tagSearch, params string[] tags)
        {
            var gameObjectTags = gameObject.Tags();
            switch (tagSearch)
            {
                case TagLogic.And:
                    return tags.All(t => gameObjectTags.Contains(t));
                case TagLogic.Or:
                    return tags.Any(t => gameObjectTags.Contains(t));
                case TagLogic.Invert:
                    return tags.All(t => !gameObjectTags.Contains(t));
                default:
                    return false;
            }
        }

        /// <summary>
        /// Remove tag(s) from gameObject.  To succeed the resulting tagPath must already exist in Unity (i.e. "T1/T2").
        /// </summary>
        /// <param name="gameObject">The gameObject.</param>
        /// <param name="tags">The tags.</param>
        public static void RemoveTags(this GameObject gameObject, params string[] tags)
        {
            gameObject.SetTags(gameObject.Tags().Except(tags).ToArray());
        }

        /// <summary>
        /// Remove tag(s) from gameObjects.  To succeed the resulting tagPath must already exist in Unity (i.e. "T1/T2").
        /// </summary>
        /// <param name="gameObjects">The gameObjects.</param>
        /// <param name="tags">The tags.</param>
        public static void RemoveTags(this IEnumerable<GameObject> gameObjects, params string[] tags)
        {
            foreach (GameObject gameObject in gameObjects)
            {
                gameObject.SetTags(gameObject.Tags().Except(tags).ToArray());
            }
        }

        /// <summary>
        /// Set tagPath on gameObjects.  To succeed the tagPath must already exist in Unity (i.e. "T1/T2").
        /// </summary>
        /// <param name="gameObjects">The gameObjects.</param>
        /// <param name="tagPath">The tag path.</param>
        public static void SetTags(this IEnumerable<GameObject> gameObjects, params string[] tagPath)
        {
            foreach (GameObject gameObject in gameObjects)
            {
                gameObject.SetTags(tagPath);
            }
        }

        /// <summary>
        /// Set tagPath on gameObject.  To succeed the tagPath must already exist in Unity (i.e. "T1/T2").
        /// </summary>
        /// <param name="gameObject">The gameObject.</param>
        /// <param name="tagPath">The tag path.</param>
        public static void SetTags(this GameObject gameObject, params string[] tagPath)
        {
            string unmatchedTag = TagService.JoinTags(tagPath.DefaultIfEmpty("Untagged"));
            string matchTagPath = TagService.JoinTags(TagService.GetTagPathMatch(tagPath).DefaultIfEmpty(unmatchedTag));

            try
            {
                Logger.Info("Set gameObject \"{0}\" tag = {1}", gameObject.name, matchTagPath);
                gameObject.tag = matchTagPath;
            }
            catch
            {
                Logger.Error("Failed to set gameObject \"{0}\" tag = {1}.  This occurs if the tag does not exist in unity.", gameObject.name, matchTagPath);
            }
        }

        /// <summary>
        /// Get the tagPaths of the specified gameObjects.
        /// </summary>
        /// <param name="gameObjects">The gameObjects.</param>
        public static IEnumerable<IEnumerable<string>> Tags(this IEnumerable<GameObject> gameObjects)
        {
            return gameObjects.Select(go => go.Tags());
        }

        /// <summary>
        /// Get the tagPaths of the specified gameObject.
        /// </summary>
        /// <param name="gameObject">The gameObject.</param>
        public static IEnumerable<string> Tags(this GameObject gameObject)
        {
            return TagService.FormatTagPath(gameObject.tag).Distinct();
        }
        #endregion
    }
}