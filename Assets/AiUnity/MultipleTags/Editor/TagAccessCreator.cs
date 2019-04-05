// ***********************************************************************
// Assembly   : Assembly-CSharp-Editor
// Company    : AiUnity
// Author     : AiDesigner
//
// Created    : 07-18-2017
// Modified   : 06-18-2018
// ***********************************************************************
using AiUnity.Common.Extensions;
using AiUnity.Common.Patterns;
using AiUnity.MultipleTags.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditorInternal;

namespace AiUnity.MultipleTags.Editor
{
    /// <summary>
    /// Creates a script that provides intuitive typed access to Unity tags.
    /// The AiUnity flagship product ScriptBuilder can generate any script using a builder.
    /// It includes a TagAccessBuilder if you wish control over generating this access script.
    /// </summary>
    /// <seealso cref="AiUnity.Common.Patterns.Singleton{AiUnity.MultipleTags.Editor.TagAccessCreator}" />
    public class TagAccessCreator : Singleton<TagAccessCreator>
    {
        #region Fields
        /// <summary> The tag hash </summary>
        private string tagHash = null;
        #endregion

        #region Properties
        /// <summary> Gets the tag service. </summary>
        private static TagService TagService { get { return TagService.Instance; } }

        /// <summary> Gets or sets the tag access string builder. </summary>
        private StringBuilder TagAccessStringBuilder { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Prevents a default instance of the <see cref="TagAccessCreator"/> class from being created.
        /// </summary>
        //private TagAccessCreator()
        public TagAccessCreator()
        {
        }
        #endregion

        #region Methods
        /// <summary>
        /// Loads the tag hash.
        /// </summary>
        public void LoadTagHash()
        {
            this.tagHash = CreateTagHash(InternalEditorUtility.tags);
        }

        /// <summary>
        /// Updates the available.
        /// </summary>
        public bool UpdateAvailable()
        {
            bool tagAccessExist = File.Exists(TagAccessFileInfo.Instance.FileInfo.FullName);
            return !tagAccessExist || this.tagHash != CreateTagHash(InternalEditorUtility.tags);
        }

        /// <summary>
        /// Creates the tag hash.
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <returns>System.String.</returns>
        public string CreateTagHash(string[] tags)
        {
            return string.Join(",", tags);
        }

        /// <summary>
        /// Called to build the tagAccess script.
        /// </summary>
        public void Create()
        {
            // Initializes the Script/Class models which you can also do yourself.
            TagAccessStringBuilder = new StringBuilder();

            CreateUsings();
            CreateClass();

            TagAccessFileInfo.Instance.Save(TagAccessStringBuilder.ToString());
            AssetDatabase.ImportAsset(TagAccessFileInfo.Instance.RelativeName);

            this.tagHash = CreateTagHash(InternalEditorUtility.tags);
        }

        /// <summary>
        /// Formats the tag paths.
        /// </summary>
        /// <param name="tagPaths">The tag paths.</param>
        /// <returns>IEnumerable&lt;IEnumerable&lt;System.String&gt;&gt;.</returns>
        public IEnumerable<IEnumerable<string>> FormatTagPaths(params string[] tagPaths)
        {
            return tagPaths.Select(s => string.Join(string.Empty, s.Split().Select(u => u.Trim().UppercaseLetter()).ToArray()))
                .Select(s => s.Split('/').Select(u => u.UppercaseLetter()).ToList().AsEnumerable()).ToList();
        }

        /// <summary>
        /// Joins the tags.
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <returns>System.String.</returns>
        public string JoinTags(IEnumerable<string> tags)
        {
            return string.Join("/", tags.ToArray());
        }

        /// <summary>
        /// Creates the usings.
        /// </summary>
        private void CreateUsings()
        {
            TagAccessStringBuilder.AppendLine("using AiUnity.Common.Attributes;");
            TagAccessStringBuilder.AppendLine("using System.Collections.Generic;");
            TagAccessStringBuilder.AppendFormat("using AiUnity.Common.Tags;{0}{0}", Environment.NewLine);
        }

        /// <summary>
        /// Creates the class.
        /// </summary>
        private void CreateClass()
        {
            // Add summary and time stamp attribute to class
            TagAccessStringBuilder.AppendLine("/// <Summary>");
            TagAccessStringBuilder.AppendLine("/// Provide strongly typed access to Unity tags.");
            TagAccessStringBuilder.AppendLine("/// <Summary>");
            TagAccessStringBuilder.AppendFormat("[GeneratedType(\"{0}\")]{1}", DateTime.Now.ToString(), Environment.NewLine);
            TagAccessStringBuilder.AppendLine("public class TagAccess : ITagAccess");
            TagAccessStringBuilder.AppendLine("{");

            // Build the various members that members of this example class
            CreateTags();
            CreateTagPaths();
            CreateGroups();

            TagAccessStringBuilder.AppendLine("}");
        }

        /// <summary>
        /// Creates the tags.
        /// </summary>
        private void CreateTags()
        {

            foreach (string tagName in TagService.AllTags.Where(t => !t.Contains('.')).Reverse())
            {
                TagAccessStringBuilder.AppendFormat("\tpublic const string {0} = \"{0}\";{1}", tagName, Environment.NewLine);
            }
        }

        /// <summary>
        /// Creates the tag paths.
        /// </summary>
        private void CreateTagPaths()
        {
            string initialize = string.Join(string.Format(",{0}\t\t", Environment.NewLine), TagService.AllTagPaths.Select(p => "\"" + JoinTags(p) + "\"").Reverse().ToArray());
            TagAccessStringBuilder.AppendFormat("{1}\tprivate static readonly List<string> tagPaths = new List<string>(){1}\t{{{1}\t\t{0}{1}\t}};{1}{1}", initialize, Environment.NewLine);
            TagAccessStringBuilder.AppendFormat("\tpublic IEnumerable<string> TagPaths {{ get {{ return tagPaths.AsReadOnly(); }} }}{0}{0}", Environment.NewLine);
        }

        /// <summary>
        /// Creates the groups.
        /// </summary>
        private void CreateGroups()
        {
            Dictionary<string, HashSet<string>> tagGroups = new Dictionary<string, HashSet<string>>();

            foreach (string tagPath in TagService.AllTags.Where(t => t.Trim('.').Contains('.')))
            {
                string[] tagGroupPath = tagPath.Split('.');

                if (!tagGroups.ContainsKey(tagGroupPath[0]))
                {
                    tagGroups[tagGroupPath[0]] = new HashSet<string>();
                }
                tagGroups[tagGroupPath[0]].Add(tagGroupPath[1]);
            }

            foreach (var tagGroupPair in tagGroups)
            {
                TagAccessStringBuilder.AppendFormat("\tpublic class {0}{1}\t{{{1}", tagGroupPair.Key, Environment.NewLine);

                foreach (string tag in tagGroupPair.Value)
                {
                    TagAccessStringBuilder.AppendFormat("\t\tpublic const string {0} = \"{1}.{0}\";{2}", tag, tagGroupPair.Key, Environment.NewLine);
                }
                TagAccessStringBuilder.AppendLine();

                TagAccessStringBuilder.AppendFormat("\t\tpublic static string Any(){0}\t\t{{{0}", Environment.NewLine);
                TagAccessStringBuilder.AppendFormat("\t\t\treturn \"{0}\";{1}\t\t}}{1}", tagGroupPair.Key, Environment.NewLine);

                TagAccessStringBuilder.AppendLine("\t}");
            }
        }
        #endregion
    }
}
