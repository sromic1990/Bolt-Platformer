// ***********************************************************************
// Assembly   : Assembly-CSharp
// Company    : AiUnity
// Author     : AiDesigner
//
// Created    : 07-07-2017
// Modified   : 10-30-2017
// ***********************************************************************
using AiUnity.Common.Extensions;
using AiUnity.Common.InternalLog;
using AiUnity.Common.Patterns;
using AiUnity.Common.Tags;
using AiUnity.Common.Types;
using AiUnity.MultipleTags.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AiUnity.MultipleTags.Core
{
    /// <summary>
    /// Provides APIs to find gameObjects by tag(s). To support this functionality there
    /// exist functions to find/search the Unity tags.
    /// </summary>
    /// <tags>MultipleTagsAPI</tags>
    public class TagService : Singleton<TagService>
    {
        #region Fields
        public readonly string[] FixedTags = { "Untagged", "Respawn", "Finish", "EditorOnly", "MainCamera", "Player", "GameController" };
        #endregion

        #region Properties
        /// <summary>All the tagPaths that exist in Unity.</summary>
        public IEnumerable<IEnumerable<string>> AllTagPaths { get; private set; }

        /// <summary>All the tags that exist in Unity.</summary>
        public IEnumerable<string> AllTags { get; private set; }

        /// <summary>The internal debug logger.</summary>
        private static IInternalLogger Logger { get { return MultipleTagsInternalLogger.Instance; } }

        /// <summary>Gets the tagPaths from an external source in a lazy manner.</summary>
        private LazyLoader<IEnumerable<IEnumerable<string>>> ExternalTagPathsLazy { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Prevents a default instance of the <see cref="TagService"/> class from being created.
        /// </summary>
        public TagService()
        {
            ExternalTagPathsLazy = new LazyLoader<IEnumerable<IEnumerable<string>>>(GetExternalTagPaths);
            RefreshTags();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Finds gameObjects by tagPath.
        /// </summary>
        /// <param name="tagPath">The tagPath to search (i.e. {T1, T2} or "T1/T2").</param>
        public GameObject[] FindGameObjectsWithTagPath(params string[] tagPath)
        {
            if (tagPath.Contains("Untagged"))
            {
                return UnityEngine.Object.FindObjectsOfType<GameObject>().Where(g => g.tag == "Untagged").ToArray();
            }
            else
            {
                try
                {
                    return GameObject.FindGameObjectsWithTag(JoinTags(tagPath));
                }
                catch (Exception)
                {
                    Logger.Debug("Tag Path={0} does not exist.", JoinTags(tagPath));
                    return new GameObject[] { };
                }
            }
        }

        /// <summary>
        /// Find gameObjects by tag pattern.
        /// </summary>
        /// <param name="tagPattern">The tag pattern (ie. TagAccess.T1, "T1", "T1 | T2", or "T.*").</param>
        public GameObject[] FindGameObjectsWithTags(string tagPattern)
        {
            return FindGameObjectsWithTags(GetTagPathMatches(tagPattern));
        }

        /// <summary>
        /// Find gameObjects by tagPaths.
        /// </summary>
        /// <param name="tagPaths">The tag paths.</param>
        public GameObject[] FindGameObjectsWithTags(IEnumerable<IEnumerable<string>> tagPaths)
        {
            return tagPaths.SelectMany(p => FindGameObjectsWithTagPath(p.ToArray())).ToArray();
        }

        /// <summary>
        /// Find gameObjects by logic and tags.
        /// </summary>
        /// <param name="tagSearch">The tag logic expression.</param>
        /// <param name="tags">The tags to be combined with the logic.</param>
        public GameObject[] FindGameObjectsWithTags(TagLogic tagSearch, params string[] tags)
        {
            IEnumerable<IEnumerable<string>> searchPaths = GetTagPathMatches(tagSearch, tags);
            return FindGameObjectsWithTags(searchPaths).ToArray();
        }

        /// <summary>
        /// Formats the tagPath to remove spaces and perform pascal casing.
        /// </summary>
        /// <param name="tagPath">The tag path to format.</param>
        public IEnumerable<string> FormatTagPath(string tagPath)
        {
            return FormatTagPaths(tagPath).SelectMany(p => p);
        }

        /// <summary>
        /// Formats the tagPaths to remove spaces and perform pascal casing.
        /// </summary>
        /// <param name="tagPaths">The tag paths to format.</param>
        public IEnumerable<IEnumerable<string>> FormatTagPaths(params string[] tagPaths)
        {
            return tagPaths
                .Select(s => string.Join(string.Empty, s.Split().Select(u => u.Trim().UppercaseLetter()).ToArray()))
                .Select(s => s.Split('/').Select(u => u.UppercaseLetter()).ToList().AsEnumerable()).ToList();
        }

        /// <summary>
        /// Get a Unity tagPath by tagPath irrespective of tag order.
        /// </summary>
        /// <param name="tagPath">The tag path to match.</param>
        public IEnumerable<string> GetTagPathMatch(IEnumerable<string> tagPath)
        {
            return GetTagPathMatches(TagLogic.And, tagPath.ToArray()).FirstOrDefault(p => p.Count() == tagPath.Count(), Enumerable.Empty<string>());
        }

        /// <summary>
        /// Get Unity tagPaths by tag pattern.
        /// </summary>
        /// <param name="findPattern">The find pattern (ie. TagAccess.T1, "T1", "T1 | T2", or "T.*").</param>
        public IEnumerable<IEnumerable<string>> GetTagPathMatches(string findPattern)
        {
            Logger.Debug("Searching tag pattern={0}", findPattern);
            string pattern = @"(([!&|^]*)([(!]*)([^!&|^()]+)(\)*))+.*$";

            Stack<TagExpression> operands = new Stack<TagExpression>();
            IEnumerable<IEnumerable<string>> tagPathMatches = Enumerable.Empty<IEnumerable<string>>();

            Match match = Regex.Match(findPattern.Replace(" ", String.Empty), pattern);

            for (int captureIndex = 0; captureIndex < match.Groups[1].Captures.Count; captureIndex++)
            {
                string subPatternClause = match.Groups[1].Captures[captureIndex].Value;
                string logicExpression = match.Groups[2].Captures[captureIndex].Value;
                string openClause = match.Groups[3].Captures[captureIndex].Value;
                string tag = match.Groups[4].Captures[captureIndex].Value;
                string closeClause = match.Groups[5].Captures[captureIndex].Value;
                TagLogic operation = TagLogic.None;

                foreach (char op in logicExpression)
                {
                    switch (op)
                    {
                        case '!':
                            operation |= TagLogic.Invert;
                            break;
                        case '&':
                            operation |= TagLogic.And;
                            break;
                        case '|':
                            operation |= TagLogic.Or;
                            break;
                        case '^':
                            operation |= TagLogic.Xor;
                            break;
                    }
                }
                Logger.Trace("Searching tag sub-pattern={0} logicClause={1} openClause={2} tagClause={3} closeClause={4} operation={5}",
                    subPatternClause, logicExpression, openClause, tag, closeClause, operation);

                for (int i = 0; i < openClause.Length; i++)
                {
                    if (openClause[i] == '!')
                    {
                        operation ^= TagLogic.Invert;
                    }
                    else if (openClause[i] == '(')
                    {
                        Logger.Trace("Push operation={0} tagPathMatches={1}", operation, JoinTagPaths(tagPathMatches));
                        operands.Push(new TagExpression(operation, tagPathMatches));
                        operation = TagLogic.None;
                    }
                }

                // Evaluate logic operation
                tagPathMatches = GetTagPathMatches(operation, tagPathMatches, tag);

                for (int closeClauseIndex = 0; closeClauseIndex < closeClause.Length; closeClauseIndex++)
                {
                    TagExpression tagExpression = operands.Pop();
                    Logger.Trace("Pop operation={0} tagPaths={1}", tagExpression.Operation, JoinTagPaths(tagExpression.TagPaths));
                    tagPathMatches = GetTagPathMatches(tagExpression.Operation, tagPathMatches, tagExpression.TagPaths);
                }
            }

            return tagPathMatches;
        }

        /// <summary>
        /// Get Unity tagPaths by logic and tags.
        /// </summary>
        /// <param name="tagLogic">The tag logic.</param>
        /// <param name="tags">The tags.</param>
        public IEnumerable<IEnumerable<string>> GetTagPathMatches(TagLogic tagLogic, params string[] tags)
        {
            IEnumerable<IEnumerable<string>> baseTagPaths = AllTagPaths;
            IEnumerable<string> splitTags = tags.SelectMany(t => t.Split('/'));
            IEnumerable<string> regexTagPatterns = splitTags.Where(p => Regex.Match(p, @"[*+?^\\{}\[\]$<>:]").Success);
            IEnumerable<string> regexTags = regexTagPatterns.SelectMany(p => AllTags.Where(t => Regex.Match(t, p).Success));
            IEnumerable<string> findTags = splitTags.Except(regexTagPatterns).Concat(regexTags).Distinct();

            IEnumerable<IEnumerable<string>> resultTagPaths = null; // Enumerable.Empty<IEnumerable<string>>();

            foreach (IEnumerable<IEnumerable<string>> tagPaths in findTags.Select(t => AllTagPaths.Where(p => ExpandTagGroups(p).Contains(t))))
            {
                resultTagPaths = GetTagPathMatches(tagLogic, tagPaths, resultTagPaths);
            }
            return resultTagPaths ?? Enumerable.Empty<IEnumerable<string>>();
        }

        /// <summary>
        /// Get Unity tagPaths by logic and tags starting from initial state.
        /// </summary>
        /// <param name="tagLogic">The tag logic.</param>
        /// <param name="op1TagPaths">The tagPaths used by the logic expression.</param>
        /// <param name="op2TagPaths">The initial state used by the logic expression.</param>
        public IEnumerable<IEnumerable<string>> GetTagPathMatches(TagLogic tagLogic, IEnumerable<IEnumerable<string>> op1TagPaths, IEnumerable<IEnumerable<string>> op2TagPaths = null)
        {
            IEnumerable<string> allTagPaths = AllTagPaths.Select(p => JoinTags(p)).ToList();
            IEnumerable<string> operand1 = op1TagPaths.Select(p => JoinTags(p)).ToList();
            IEnumerable<string> operand2 = op2TagPaths != null ? op2TagPaths.Select(p => JoinTags(p)) : Enumerable.Empty<string>();
            TagLogic tagOperations = op2TagPaths == null ? tagLogic & TagLogic.Invert : tagLogic;

            foreach (TagLogic tagOperation in tagOperations.GetFlags())
            {
                switch (tagOperation)
                {
                    case TagLogic.Invert:
                        operand1 = allTagPaths.Except(operand1);
                        break;
                    case TagLogic.And:
                        operand1 = operand1.Intersect(operand2);
                        break;
                    case TagLogic.Or:
                        operand1 = operand1.Union(operand2);
                        break;
                    case TagLogic.Xor:
                        operand1 = operand1.Except(operand1.Intersect(operand2));
                        break;
                    default:
                        operand1 = Enumerable.Empty<string>();
                        break;
                }
            }
            Logger.Trace("tagLogic={0} Result={1}{4}Operand1={2}{4}Operand2={3}",tagLogic, string.Join(",", operand1.ToArray()), JoinTagPaths(op2TagPaths), JoinTagPaths(op1TagPaths), Environment.NewLine);

            return operand1.Select(p => p.Split('/').AsEnumerable());
        }

        /// <summary>
        /// Joins the tags with backslashes to form a tagPath.
        /// </summary>
        /// <param name="tags">The tags.</param>
        public string JoinTags(IEnumerable<string> tags)
        {
            return string.Join("/", tags.ToArray());
        }

        /// <summary>
        /// Refreshes the tags so that the APIs have an accurate list of Unity tags.
        /// The editor has direct access to the internal Unity tags.
        /// In editor play mode and in players the external TagAccess is used to get Unity tagPaths.
        /// In the absence of a TagAccess the Unity tags will be constructed from the gameObjects.
        /// </summary>
        public void RefreshTags()
        {
#if UNITY_EDITOR
            IEnumerable<IEnumerable<string>> InternalTagPaths = FormatTagPaths(UnityEditorInternal.InternalEditorUtility.tags.Reverse().ToArray());
            AllTagPaths = UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode ? ExternalTagPathsLazy.Value : InternalTagPaths;
#else
            AllTagPaths = ExternalTagPathsLazy.Value;
#endif
            AllTags = AllTagPaths.SelectMany(p => p).Distinct().ToList();
        }

        /// <summary>
        /// Expands the tag groups.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        private IEnumerable<string> ExpandTagGroups(IEnumerable<string> tagPath)
        {
            return tagPath.Concat(tagPath.Where(t => t.Contains('.')).SelectMany(t => t.Split('.').SkipLast()));
        }

        /// <summary>
        /// Get the TagPaths from the external TagAccess.cs if available.
        /// In the absence of a TagAccess the Unity tags will be constructed from the gameObjects.
        /// </summary>
        private IEnumerable<IEnumerable<string>> GetExternalTagPaths()
        {
            IEnumerable<string> searchAssemblyNames = new List<string>() { "Assembly-CSharp" };
            IEnumerable<Assembly> searchAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => searchAssemblyNames.Any(t => a.FullName.StartsWith(t)));
            IEnumerable<Type> tagAccessTypes = searchAssemblies.SelectMany(a => a.GetTypes()).Where(t => typeof(ITagAccess).IsAssignableFrom(t) && t.IsClass);
            IEnumerable<IEnumerable<string>> tagPaths;

            if (tagAccessTypes.Count() == 1)
            {
                ITagAccess tagAccess = (ITagAccess)Activator.CreateInstance(tagAccessTypes.First());
                Logger.Info("Using class \"{0}\" class to discover unity tags at runtime.", tagAccess.GetType().FullName);
                tagPaths = SplitTagPaths(tagAccess.TagPaths.Reverse().ToArray());
            }
            else
            {
                if (tagAccessTypes.Count() == 0)
                {
                    Logger.Warn("Please create or generate an accurate ITagAccess implementation, which provides a list of Unity tags at runtime.{0}" +
                        "-An accurate list of Unity tags is required for MultipleTags find APIs to function properly.{0}" +
                        "-MultipleTags GUI options exist to manually or automatically generate a TagAccess.cs script.{0}" +
                        "-In the absence of ITagAccess MultipleTags, will scan all gameObjects to aggregate a list of tags.{0}", Environment.NewLine);
                }
                if (tagAccessTypes.Count() > 1)
                {
                    string userTagAccessNames = string.Join(Environment.NewLine, tagAccessTypes.Select(t => t.FullName).ToArray());
                    Logger.Error("Please provide a single implementing ITagAccess (List below):" + Environment.NewLine + userTagAccessNames);
                }
                Logger.Info("Discovering Unity tags by analyzing all instantiated gameObjects." +
                    "  This approach will not be aware of tags introduced from any runtime instantiated gameObjects.");

                tagPaths = FormatTagPaths(this.FixedTags.Concat(SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject.tag))
                    .Where(t => !string.IsNullOrEmpty(t))).Distinct().ToArray());
            }

            Logger.Debug("Discovered tagPaths={0}", JoinTagPaths(tagPaths));
            return tagPaths;
        }

        /// <summary>
        /// Gets the tagPaths by logic and tag starting from initial tags.
        /// </summary>
        /// <param name="tagLogic">The tag logic.</param>
        /// <param name="startTagPaths">The start tag paths.</param>
        /// <param name="tag">The tag used by logic.</param>
        private IEnumerable<IEnumerable<string>> GetTagPathMatches(TagLogic tagLogic, IEnumerable<IEnumerable<string>> startTagPaths, string tag)
        {
            return GetTagPathMatches(tagLogic, GetTagPathMatches(TagLogic.Or, tag), startTagPaths);
        }

        /// <summary>
        /// Joins the tagPaths for printing purposes.
        /// </summary>
        /// <param name="tagPaths">The tag paths.</param>
        private string JoinTagPaths(IEnumerable<IEnumerable<string>> tagPaths)
        {
            return tagPaths == null ? "null" : string.Join(", ", tagPaths.Select(p => JoinTags(p)).ToArray());
        }

        /// <summary>
        /// Splits the tagPaths.
        /// </summary>
        /// <param name="tagPaths">The tag paths.</param>
        /// <returns>IEnumerable&lt;IEnumerable&lt;System.String&gt;&gt;.</returns>
        private IEnumerable<IEnumerable<string>> SplitTagPaths(params string[] tagPaths)
        {
            return tagPaths.Select(s => s.Split('/').AsEnumerable()).ToList();
        }
        #endregion
    }
}