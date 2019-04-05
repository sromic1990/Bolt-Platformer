// ***********************************************************************
// Assembly   : Assembly-CSharp-Editor
// Company    : AiUnity
// Author     : AiDesigner
//
// Created    : 07-07-2017
// Modified   : 05-18-2018
// ***********************************************************************
using AiUnity.Common.Extensions;
using AiUnity.Common.InternalLog;
using AiUnity.Common.Patterns;
using AiUnity.MultipleTags.Common;
using AiUnity.MultipleTags.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AiUnity.MultipleTags.Editor
{
    /// <summary>
    /// Utilized by the MultipleTags GUI to manage tags.
    /// This class should not be utilized by the end user directly.
    /// </summary>
    /// <seealso cref="AiUnity.Common.Patterns.Singleton{AiUnity.MultipleTags.Editor.TagManager}" />
    public class TagManager : Singleton<TagManager>
    {
        #region Constants
        private const int MaxTags = 10000;
        #endregion

        #region Fields
        public GameObject[] allGameObjects;
        public bool groupExpand = true;
        public bool tagExpand = false;
        public Dictionary<string, int> TagPathCount = new Dictionary<string, int>();
        #endregion

        #region Properties
        /// <summary>Gets the tag service.</summary>
        private static TagService TagService { get { return TagService.Instance; } }

        /// <summary>Gets all fixed tag paths.</summary>
        public IEnumerable<IEnumerable<string>> AllFixedTagPaths { get { return TagService.AllTagPaths.Where(p => p.Count() == 1 && TagService.FixedTags.Contains(p.First())); } }

        /// <summary>Gets all fixed tags.</summary>
        public IEnumerable<string> AllFixedTags { get { return TagService.AllTags.Where(t => TagService.FixedTags.Contains(t)); } }

        /// <summary>Gets all user tag paths.</summary>
        public IEnumerable<IEnumerable<string>> AllUserTagPaths { get { return TagService.AllTagPaths.Except(AllFixedTagPaths); } }

        /// <summary>Gets all user tags.</summary>
        public IEnumerable<string> AllUserTags { get { return TagService.AllTags.Where(t => !TagService.FixedTags.Contains(t)); } }

        /// <summary>Gets or sets a value indicating whether [automatic tag optimize].</summary>
        public bool AutoTagOptimize { get; set; }

        /// <summary>Gets the priority tags.</summary>
        public List<string> priorityTags { get; private set; }

        /// <summary>Internal logger singleton.</summary>
        private static IInternalLogger Logger { get { return MultipleTagsInternalLogger.Instance; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Prevents a default instance of the <see cref="TagManager"/> class from being created.
        /// </summary>
        //private TagManager()
        public TagManager()
        {
            //EditorApplication.playmodeStateChanged += PlayModeStateChanged;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Refreshes the tags.
        /// </summary>
        public void RefreshTags()
        {
            if (TagService.InstanceExists)
            {
                TagService.RefreshTags();
            }

            this.allGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

            priorityTags = new List<string>();
            foreach (List<string> tagPath in TagService.AllTagPaths.Where(p => p.Count() > 1).Reverse().Select(p => p.ToList()).ToList())
            {
                foreach (string tag in tagPath.SkipLast().Where(t => !priorityTags.Any(p => p == t)))
                {
                    priorityTags.InsertRelative(tagPath, tag);
                }
            }

            this.TagPathCount.Clear();
            foreach (GameObject gameObject in this.allGameObjects)
            {
                string tag = TagService.JoinTags(gameObject.Tags());
                int tagPathCount = 0;
                this.TagPathCount.TryGetValue(tag, out tagPathCount);
                this.TagPathCount[tag] = ++tagPathCount;
            }
        }

        /// <summary>
        /// Adds the tags.
        /// </summary>
        /// <param name="gameObjects">The game objects.</param>
        /// <param name="tags">The tags.</param>
        public void AddTags(IEnumerable<GameObject> gameObjects, params string[] tags)
        {
            foreach (GameObject gameObject in gameObjects)
            {
                AddTags(gameObject, tags);
            }
        }

        /// <summary>
        /// Adds the tags.
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        /// <param name="tags">The tags.</param>
        public void AddTags(GameObject gameObject, params string[] tags)
        {
            IEnumerable<string> gameObjectTagPaths = gameObject.Tags().Where(t => t != "Untagged");
            IEnumerable<string> updatedTags = AddTags(gameObjectTagPaths.Concat(tags).ToArray());
            gameObject.SetTags(updatedTags.ToArray());

            if (!this.tagExpand)
            {
                RemoveTags(gameObjectTagPaths.ToArray());
            }
        }

        /// <summary>
        /// Add a tagPath to Unity.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        /// <remarks>http://answers.unity3d.com/questions/33597/is-it-possible-to-create-a-tag-programmatically.html</remarks>
        public IEnumerable<string> AddTags(params string[] tagPath)
        {
            Logger.Trace("Try adding unity tag={0}.", TagService.JoinTags(tagPath));

            // Saves off any manual changes made to the Unity Tags
            AssetDatabase.SaveAssets();

            IEnumerable<string> matchTags = (tagPath.Any() && !tagPath.Contains("Untagged")) ? TagService.GetTagPathMatch(tagPath) : new List<string>() { "Untagged" };

            if (matchTags.Any())
            {
                Logger.Trace("Skip adding unity tag \"{0}\" which exists as \"{1}\".", TagService.JoinTags(tagPath), TagService.JoinTags(matchTags));
                return matchTags;
            }
            else
            {
                List<string> revisedTags = new List<string>(tagPath.Where(t => t.Contains('/')).SelectMany(t => t.Split('/')));

                foreach (string tag in tagPath.Where(t => !t.Contains('/')))
                {
                    revisedTags.InsertRelative(priorityTags, tag);
                }

                SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                SerializedProperty tagsProp = tagManager.FindProperty("tags");

                if (tagsProp.arraySize >= MaxTags)
                {
                    Logger.Error("Exceeded Unity max tag count={0}/{1}", tagsProp.arraySize, MaxTags);
                    return new List<string>() { "Untagged" };
                }

                string revisedTagPath = TagService.JoinTags(revisedTags);

                // Check if tag already exist in Unity
                if (!PropertyExists(tagsProp, 0, tagsProp.arraySize, revisedTagPath))
                {
                    Logger.Info("Adding Unity Tag={0}", revisedTagPath);
                    int index = tagsProp.arraySize;
                    tagsProp.InsertArrayElementAtIndex(index);
                    SerializedProperty sp = tagsProp.GetArrayElementAtIndex(index);
                    sp.stringValue = revisedTagPath;

                    // Save settings
                    tagManager.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();

                    if (AutoTagOptimize)
                    {
                        OptimizeTags(revisedTags);
                    }
                }
                else
                {
                    Logger.Assert("Tag \"{0}\" already exists in Unity", revisedTagPath);
                }
                return revisedTags;
            }
        }

        /// <summary>
        /// Removes the tags.
        /// </summary>
        /// <param name="gameObjects">The game objects.</param>
        /// <param name="tags">The tags.</param>
        public IEnumerable<string> RemoveTags(IEnumerable<GameObject> gameObjects, params string[] tags)
        {
            foreach (GameObject gameObject in gameObjects)
            {
                IEnumerable<string> gameObjectTags = gameObject.Tags();
                IEnumerable<string> updatedTags = AddTags(gameObjectTags.Except(tags).ToArray());
                gameObject.SetTags(updatedTags.ToArray());
                if (!this.tagExpand)
                {
                    RemoveTags(gameObjectTags.ToArray());
                }

                return updatedTags;
            }
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Uglifies the tag path.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        public string UglifyTagPath(string tagPath)
        {
            return InternalEditorUtility.tags.FirstOrDefault(p => (tagPath.ToLower()) == (p.Replace(" ", string.Empty).ToLower()));
        }

        /// <summary>
        /// Remove the tagPath from Unity.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        public IEnumerable<string> RemoveTags(params string[] tagPath)
        {
            // Saves off any manual changes made to the Unity Tags
            AssetDatabase.SaveAssets();

            IEnumerable<string> matchTagPath = TagService.GetTagPathMatch(tagPath);
            string unityTagPath = UglifyTagPath(TagService.JoinTags(matchTagPath));

            if (string.IsNullOrEmpty(unityTagPath))
            {
                Logger.Info("Unable to remove unmatched tag={0}", unityTagPath);
            }
            else if (TagService.FindGameObjectsWithTagPath(unityTagPath).Any())
            {
                Logger.Info("Unable to remove in use tag={0}", unityTagPath);
            }
            else
            {
                SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                SerializedProperty tagsProp = tagManager.FindProperty("tags");

                if (PropertyExists(tagsProp, 0, tagsProp.arraySize, unityTagPath))
                {
                    SerializedProperty sp;

                    for (int i = 0, j = tagsProp.arraySize; i < j; i++)
                    {
                        sp = tagsProp.GetArrayElementAtIndex(i);
                        if (sp.stringValue == unityTagPath)
                        {
                            Logger.Info("Removing unity tag={0}", unityTagPath);
                            // Set value to empty instead of deleting to keep Unity happy
                            sp.stringValue = string.Empty;
                            tagManager.ApplyModifiedProperties();
                            AssetDatabase.SaveAssets();
                            return matchTagPath;
                        }
                    }
                }
                Logger.Debug("Unable to locate and remove tag={0}", unityTagPath);
            }
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Optimizes the tags.
        /// </summary>
        /// <param name="tagPaths">The tag paths.</param>
        public void OptimizeTags(params IEnumerable<string>[] tagPaths)
        {
            RefreshTags();

            IEnumerable<IEnumerable<string>> optimizeTagPaths = tagPaths.Length != 0 ? tagPaths : AllUserTagPaths;
            IEnumerable<string> optimizeTags = optimizeTagPaths.SelectMany(p => p).Distinct();

            Logger.Info("Optimizing tagPaths={0}", string.Join(", ", optimizeTagPaths.Select(p => string.Join("/", p.ToArray())).ToArray()));

            foreach (string tagPath in optimizeTags)
            {
                string tagPathFormatted = TagService.JoinTags(TagService.FormatTagPath(tagPath));

                if (tagPath != tagPathFormatted)
                {
                    Logger.Info("Format tag to pascal casing tag({0})={1} -> tag={2}", GetTagIndex(tagPath), tagPath, tagPathFormatted);
                    RemoveTags(tagPath);
                    AddTags(tagPathFormatted);
                }
            }
            RefreshTags();

            foreach (IEnumerable<string> tagPath in optimizeTagPaths)
            {
                int unityTagIndex = GetTagIndex(tagPath.ToArray());
                IEnumerable<string> matchTagPath = TagService.GetTagPathMatch(tagPath);

                if (!matchTagPath.SequenceEqual(tagPath))
                {
                    foreach (GameObject gameObject in TagService.FindGameObjectsWithTagPath(tagPath.ToArray()))
                    {
                        Logger.Info("Consolidate gameObject={0} tag({1})={2} -> tag{3}", gameObject.name, unityTagIndex, TagService.JoinTags(tagPath), TagService.JoinTags(matchTagPath));
                    }

                    // Remove duplicate tag paths, which occurs if tags are in different order
                    Logger.Info("Remove duplicate tag({0})={1} which duplicates tag={2}", unityTagIndex, TagService.JoinTags(tagPath), TagService.JoinTags(matchTagPath));
                    RemoveTags(TagService.JoinTags(tagPath));
                    continue;
                }

                // Ensure all subpaths exists as tags
                if (this.tagExpand)
                {
                    for (int j = 1; j < tagPath.Count(); j++)
                    {
                        if (!TagService.GetTagPathMatch(tagPath.Take(j)).Any())
                        {
                            Logger.Info("Expand tag path tag({0})={1} -> tag={2} ", unityTagIndex, TagService.JoinTags(tagPath), TagService.JoinTags(tagPath.Take(j)));
                            AddTags(tagPath.Take(j).ToArray());
                        }
                    }
                }
            }

            RefreshTags();

            // Expand tags to include all tag group possibilities
            if (this.groupExpand)
            {
                Dictionary<string, HashSet<string>> tagGroups = new Dictionary<string, HashSet<string>>();

                // Preprocess tags to learn tag groups
                foreach (string tagPath in AllUserTags.Where(t => t.Contains('.')))
                {
                    HashSet<string> tagGroupHashSet;
                    if (!tagGroups.TryGetValue(tagPath.Before('.'), out tagGroupHashSet))
                    {
                        tagGroups[tagPath.Before('.')] = tagGroupHashSet = new HashSet<string>();
                    }
                    tagGroupHashSet.Add(tagPath.After('.'));
                }

                foreach (IEnumerable<string> tagPath in optimizeTagPaths)
                {
                    int unityTagIndex = GetTagIndex(tagPath.ToArray());

                    for (int j = 0; j < tagPath.Count(); j++)
                    {
                        string tag = tagPath.ElementAt(j);

                        if (tag.Contains("."))
                        {
                            IEnumerable<string> subTagPath = tagPath.Take(j);
                            HashSet<string> tagGroupHashSet;
                            string tagGroupRoot = tag.Before(".");
                            if (tagGroups.TryGetValue(tagGroupRoot, out tagGroupHashSet))
                            {
                                foreach (string tagGroupName in tagGroupHashSet)
                                {
                                    string[] tagGroupFullName = subTagPath.MyAppend(tagGroupRoot + "." + tagGroupName).ToArray();
                                    if (!TagService.GetTagPathMatch(tagGroupFullName).Any())
                                    {
                                        Logger.Info("Expand tag group tag({0})={1} -> tag={2}", unityTagIndex, TagService.JoinTags(tagPath), TagService.JoinTags(tagGroupFullName));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            RefreshTags();
        }

        /// <summary>
        /// Plays the mode state changed.
        /// </summary>
        private void PlayModeStateChanged(PlayModeStateChange state)
        {
            //if (EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying)
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                RefreshTags();
            }
        }

        /// <summary>
        /// Gets the index of the tag.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        private int GetTagIndex(params string[] tagPath)
        {
            return AllUserTagPaths.Reverse().Select(p => TagService.JoinTags(p)).IndexOf(TagService.JoinTags(tagPath));
        }

        /// <summary>
        /// Checks if the value exists in the property.
        /// </summary>
        /// <param name="property">Property.</param>
        /// <param name="start">Start.</param>
        /// <param name="end">End.</param>
        /// <param name="value">Value.</param>
        private bool PropertyExists(SerializedProperty property, int start, int end, string value)
        {
            for (int i = start; i < end; i++)
            {
                SerializedProperty t = property.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(value))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}