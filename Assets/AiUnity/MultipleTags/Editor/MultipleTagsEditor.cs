// ***********************************************************************
// Assembly         : Assembly-CSharp-Editor
// Author           : AiDesigner
//
// Created          : 05-12-2017
// Modified         : 05-18-2018
// ***********************************************************************
using AiUnity.Common.Editor.ModalWindow;
using AiUnity.Common.Editor.Styles;
using AiUnity.Common.Extensions;
using AiUnity.Common.InternalLog;
using AiUnity.Common.Log;
using AiUnity.MultipleTags.Common;
using AiUnity.MultipleTags.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using AiUnity.Common.Utilities;

namespace AiUnity.MultipleTags.Editor
{
    /// <summary>
    /// MultipleTags GUI to find, manage, and assign Unity tags.
    /// </summary>
    /// <seealso cref="UnityEditor.EditorWindow" />
    [Serializable]
    public class MultipleTagsEditor : EditorWindow
    {
        #region Constants
        // Tooltip for Internal Level selection GUI
        private const string InternalLevelsTooltip = @"Specifies which logging levels are enabled for Multiple Tags internal messages (Debug feature).";
        #endregion

        #region Fields
        public List<string> searchTags = new List<string>();
        private GUIContent findGameObjectExpression = new GUIContent();
        private bool findGameObjectExpressionValid = true;
        private AnimBool findGameObjectFoldout = new AnimBool(false);
        private Rect findGameObjectMenuRect = new Rect();
        private IEnumerable<GameObject> findGameObjects = Enumerable.Empty<GameObject>();
        private GUIContent findTagExpression = new GUIContent(string.Empty);
        private bool findTagExpressionValid = true;
        private AnimBool findTagFoldout = new AnimBool(false);
        private IEnumerable<IEnumerable<string>> findTagPaths = Enumerable.Empty<IEnumerable<string>>();
        private GUIContent internalLevelsContent = new GUIContent("Internal levels", InternalLevelsTooltip);
        private AnimBool optionsFoldout = new AnimBool(false);
        private TagCategory tagCategory = TagCategory.All;
        private Vector2 tagFindScroll = Vector2.zero;
        private Vector2 tagGameObjectScroll = Vector2.zero;
        private Dictionary<string, bool> TagSelected = new Dictionary<string, bool>();
        #endregion

        #region Properties
        /// <summary>Gets the tag manager.</summary>
        private static TagManager TagManager { get { return TagManager.Instance; } }

        /// <summary>Gets the tag service.</summary>
        private static TagService TagService { get { return TagService.Instance; } }

        // Internal logger singleton
        /// <summary>Gets the logger.</summary>
        private static IInternalLogger Logger { get { return MultipleTagsInternalLogger.Instance; } }
        #endregion

        #region Methods
        /// <summary>
        /// Creates the menu item.
        /// </summary>
        /// <param name="windowResult">The window result.</param>
        /// <param name="createTagData">The create tag data.</param>
        /// <param name="addTagAction">The add tag action.</param>
        public void CreateTagItem(WindowResult windowResult, CreateTagData createTagData, Action<string> addTagAction)
        {
            if (createTagData == null || string.IsNullOrEmpty(createTagData.Tags) || windowResult == WindowResult.Cancel || windowResult == WindowResult.Invalid)
            {
                Logger.Debug("Aborting tag creation");
                return;
            }

            foreach (string tag in createTagData.Tags.Split(' ', ',', ';'))
            {
                addTagAction(tag);
            }
        }

        /// <summary>
        /// Unity Menu entry to launch MultipleTags editor control window.
        /// </summary>
        [MenuItem("Tools/AiUnity/MultipleTags/Control Panel")]
        private static void ControlPanelMenu()
        {
            GetWindow<MultipleTagsEditor>("MultipleTags");
        }

        /// <summary>
        /// Unity Menu entry to launch AiUnity forums website.
        /// </summary>
        [MenuItem("Tools/AiUnity/MultipleTags/Forums")]
        private static void ForumsMenu()
        {
            Application.OpenURL("https://forum.aiunity.com/categories");
        }

        /// <summary>
        /// Unity Menu entry to launch MultipleTags help website.
        /// </summary>
        [MenuItem("Tools/AiUnity/MultipleTags/Help")]
        private static void HelpMenu()
        {
            Application.OpenURL("http://aiunity.com/products/multiple-tags");
        }

        /// <summary>
        /// Called when [enable].
        /// </summary>
        void OnEnable()
        {
            PlayerPrefs.SetInt("AiUnityIsProSkin", Convert.ToInt32(EditorGUIUtility.isProSkin));
            this.findTagFoldout.valueChanged.AddListener(Repaint);
            this.findGameObjectFoldout.valueChanged.AddListener(Repaint);
            this.optionsFoldout.valueChanged.AddListener(Repaint);
        }

        /// <summary>
        /// Called when editor window gains focus.
        /// </summary>
        void OnFocus()
        {
            RefreshTags();
        }

        /// <summary>
        /// Called when [selection change].
        /// </summary>
        void OnSelectionChange()
        {
            // Repaint even if not in focus
            // http://answers.unity3d.com/questions/38783/repainting-an-editorwindow-when-its-not-the-curren.html
            Repaint();
        }

        /// <summary>
        /// Refreshes the tags.
        /// </summary>
        void RefreshTags()
        {
            TagManager.RefreshTags();
            Repaint();
        }


        /// <summary>
        /// Called when [GUI].
        /// </summary>
        void OnGUI()
        {
            EditorGUILayout.Space();

            UnityEngine.Object activeObject = Selection.activeObject;
            GameObject[] activeGameObjects = Selection.gameObjects;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.Format("Tags ({0}/{1} gameObjects):", activeGameObjects.Count(), TagManager.allGameObjects.Count()));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(string.Empty, CustomEditorStyles.HelpIconStyle))
            {
                Application.OpenURL("http://aiunity.com/products/multiple-tags/manual#gui-tags");
            }
            EditorGUILayout.EndHorizontal();

            if (activeGameObjects.Any())
            {
                IEnumerable<string> activeTags = activeGameObjects.Skip(1).Aggregate(new HashSet<string>(activeGameObjects.First().Tags()), (h, e) => { h.IntersectWith(e.Tags()); return h; });
                CreateGUITags(activeTags, tag => MinusTagGameObjects(tag, activeGameObjects), tag => AddTagGameObjects(tag, activeGameObjects));
            }
            else
            {
                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                labelStyle.fontStyle = FontStyle.Italic;
                EditorGUILayout.LabelField("Please select gameObject(s) in Hierarchy", labelStyle);
            }
            EditorGUILayout.Space();

            // Draw horizontal separator
            GUILayout.Box(GUIContent.none, CustomEditorStyles.EditorLine, GUILayout.ExpandWidth(true), GUILayout.Height(1f));
            EditorGUILayout.Space();

            // Draw tag search
            DrawTagFind();

            // Draw gameObject search
            DrawGameObjectFind();

            // Draw options
            DrawOptions();

            GUILayout.FlexibleSpace();
        }

        /// <summary>
        /// Draws the game object find.
        /// </summary>
        private void DrawGameObjectFind()
        {
            EditorGUILayout.BeginHorizontal();
            GUIContent searchFoldoutContent = new GUIContent(string.Format("Find gameObjects"));
            GUIStyle searchFoldoutStyle = new GUIStyle(EditorStyles.foldout);
            this.findGameObjectFoldout.target = EditorGUILayout.Foldout(this.findGameObjectFoldout.target, searchFoldoutContent, searchFoldoutStyle);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(string.Empty, CustomEditorStyles.HelpIconStyle))
            {
                Application.OpenURL("http://aiunity.com/products/multiple-tags/manual#gui-find-game-objects");
            }
            EditorGUILayout.EndHorizontal();

            if (EditorGUILayout.BeginFadeGroup(this.findGameObjectFoldout.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                GUIContent findGameObjectContent = new GUIContent("Tag expression", "Find gameObjects containing tags matching this expression.  The expression can use text, regex, and boolean logic.  Click the + sign to see available syntax and tags.  The expression complexity can range from \"Tag1\" to \"(Tag1 & Tag2.*) | Tag3\".  An empty expression is a special case that denotes all gameObjects that are not \"Untagged\".");
                EditorGUILayout.PrefixLabel(findGameObjectContent);
                GUIStyle findGameObjectStyle = new GUIStyle(EditorStyles.textField) { wordWrap = true };

                if (!this.findGameObjectExpressionValid)
                {
                    GUI.backgroundColor = Color.red;
                }

                GUI.SetNextControlName("FindGameObjectExpression");
                this.findGameObjectExpression.text = GUILayout.TextField(this.findGameObjectExpression.text, findGameObjectStyle);

                GUI.backgroundColor = Color.white;
                bool addLayout = GUILayout.Button(string.Empty, CustomEditorStyles.PlusIconStyle);

                if (Event.current.type == EventType.Repaint)
                {
                    this.findGameObjectMenuRect = GUILayoutUtility.GetLastRect();
                }
                if (addLayout)
                {
                    EditorGUI.FocusTextInControl("FindGameObjectExpression");
                    GenericMenu findMenu = CreateFindMenu(this.findGameObjectExpression);
                    findMenu.DropDown(this.findGameObjectMenuRect);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;

                try
                {
                    Regex.Match(string.Empty, this.findGameObjectExpression.text);
                    this.findGameObjects = string.IsNullOrEmpty(this.findGameObjectExpression.text) ? TagManager.allGameObjects.Where(g => g != null && g.tag != "Untagged") : TagService.FindGameObjectsWithTags(this.findGameObjectExpression.text);
                    this.findGameObjectExpressionValid = true;
                }
                catch (ArgumentException)
                {
                    this.findGameObjectExpressionValid = false;
                }

                if (this.findGameObjects.Any())
                {
                    EditorGUILayout.Space();
                    this.tagGameObjectScroll = EditorGUILayout.BeginScrollView(this.tagGameObjectScroll, false, false);

                    GUIStyle tagHeaderStyle = new GUIStyle(EditorStyles.label);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    EditorGUILayout.BeginVertical("box");

                    bool gameObjectSelectAll = !this.findGameObjects.Cast<UnityEngine.Object>().Except(Selection.objects).Any();
                    bool gameObjectSelectAllUpdate = EditorGUILayout.ToggleLeft("Select All", gameObjectSelectAll, tagHeaderStyle);
                    if (gameObjectSelectAllUpdate ^ gameObjectSelectAll)
                    {
                        if (gameObjectSelectAllUpdate)
                        {
                            Selection.objects = Selection.objects.Concat(this.findGameObjects.Cast<UnityEngine.Object>()).ToArray();
                        }
                        else
                        {
                            Selection.objects = Selection.objects.Except(this.findGameObjects.Cast<UnityEngine.Object>()).ToArray();
                        }
                    }

                    GUIContent longestGameObjectContent = new GUIContent(this.findGameObjects.Select(g => g.name).OrderByDescending(n => n.Length).First());
                    EditorGUIUtility.labelWidth = Math.Max(10, (int)tagHeaderStyle.CalcSize(longestGameObjectContent).x - 30);
                    int tagOffset = (int)EditorGUIUtility.labelWidth + 40 + 50;

                    foreach (GameObject gameObject in this.findGameObjects)
                    {
                        EditorGUILayout.BeginHorizontal();

                        bool gameObjectSelected = Selection.objects.Contains(gameObject);
                        GUIContent gameObjectSelectedContent = new GUIContent(gameObject.name);
                        bool gameObjectSelectedUpdate = EditorGUILayout.ToggleLeft(gameObjectSelectedContent, gameObjectSelected, tagHeaderStyle, GUILayout.ExpandWidth(false));

                        if (gameObjectSelected ^ gameObjectSelectedUpdate)
                        {
                            if (gameObjectSelectedUpdate)
                            {
                                Selection.objects = Selection.objects.MyAppend(gameObject).ToArray();
                            }
                            else
                            {
                                Selection.objects = Selection.objects.Where(g => g != gameObject).ToArray();
                            }
                        }
                        EditorGUILayout.BeginVertical();
                        CreateGUITags(gameObject.Tags(), tag => MinusTagGameObjects(tag, gameObject), tag => AddTagGameObjects(tag, gameObject), tagOffset);
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUIUtility.labelWidth = 0;

                    EditorGUILayout.Space();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndFadeGroup();
        }

        /// <summary>
        /// Draws the tag find.
        /// </summary>
        private void DrawTagFind()
        {
            EditorGUILayout.BeginHorizontal();
            GUIContent findTagFoldoutContent = new GUIContent(string.Format("Find tags"));
            GUIStyle findTagFoldoutStyle = new GUIStyle(EditorStyles.foldout);
            this.findTagFoldout.target = EditorGUILayout.Foldout(this.findTagFoldout.target, findTagFoldoutContent, findTagFoldoutStyle);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(string.Empty, CustomEditorStyles.HelpIconStyle))
            {
                Application.OpenURL("http://aiunity.com/products/multiple-tags/manual#gui-find-tags");
            }
            EditorGUILayout.EndHorizontal();

            if (EditorGUILayout.BeginFadeGroup(this.findTagFoldout.faded))
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                GUIStyle findCategoryStyle = new GUIStyle(EditorStyles.radioButton);
                findCategoryStyle.margin.left = 0;
                findCategoryStyle.margin.right = 0;
                EditorGUILayout.PrefixLabel("Tag category");

                string[] tagCategories = Enum.GetNames(typeof(TagCategory));
                this.tagCategory = (TagCategory)GUILayout.SelectionGrid((int)this.tagCategory, tagCategories, tagCategories.Length, findCategoryStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUIContent findTagContent = new GUIContent("Tag expression", "Find Unity tags matching this expression.  The expression can use text, regex, and boolean logic.  Click the + sign to see available syntax and tags.  The expression complexity can range from \"Tag1\" to \"(Tag1 & Tag2.*) | Tag3\".  An empty expression is a special case that denotes all tags (ie. .*).");

                EditorGUILayout.PrefixLabel(findTagContent);
                GUIStyle findTagStyle = new GUIStyle(EditorStyles.textField) { wordWrap = true };

                foreach (string tagPath in this.TagSelected.Where(p => p.Value).Select(p => p.Key).Except(this.findTagPaths.Select(p => TagService.JoinTags(p))).ToList())
                {
                    this.TagSelected[tagPath] = false;
                }

                if (!this.findTagExpressionValid)
                {
                    GUI.backgroundColor = Color.red;
                }

                GUI.SetNextControlName("FindTagExpression");
                this.findTagExpression.text = GUILayout.TextField(this.findTagExpression.text, findTagStyle);
                GUI.backgroundColor = Color.white;
                bool addLayout = GUILayout.Button(string.Empty, CustomEditorStyles.PlusIconStyle);

                if (Event.current.type == EventType.Repaint)
                {
                    this.findGameObjectMenuRect = GUILayoutUtility.GetLastRect();
                }
                if (addLayout)
                {
                    EditorGUI.FocusTextInControl("FindTagExpression");
                    GenericMenu findMenu = CreateFindMenu(this.findTagExpression);
                    findMenu.DropDown(this.findGameObjectMenuRect);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;

                try
                {
                    Regex.Match(string.Empty, this.findTagExpression.text);
                    this.findTagPaths = string.IsNullOrEmpty(this.findTagExpression.text) ? TagService.AllTagPaths : TagService.GetTagPathMatches(this.findTagExpression.text);
                    this.findTagExpressionValid = true;
                }
                catch (ArgumentException)
                {
                    this.findTagExpressionValid = false;
                }

                if (this.findTagPaths.Any())
                {
                    EditorGUILayout.Space();
                    this.tagFindScroll = EditorGUILayout.BeginScrollView(this.tagFindScroll, false, false);
                    GUIStyle tagHeaderStyle = new GUIStyle(EditorStyles.label);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    EditorGUILayout.BeginVertical("box");

                    bool tagSelectAll = this.TagSelected.All(p => p.Value || TagService.FixedTags.Any(f => f == p.Key));
                    bool tagSelectAllUpdate = EditorGUILayout.ToggleLeft("Select All", tagSelectAll, tagHeaderStyle);
                    if (tagSelectAllUpdate ^ tagSelectAll)
                    {
                        this.TagSelected = this.TagSelected.ToDictionary(p => p.Key, p => tagSelectAllUpdate && !TagService.FixedTags.Any(f => f == p.Key));
                    }

                    foreach (string tagPath in this.findTagPaths.Select(p => TagService.JoinTags(p)).Reverse())
                    {
                        EditorGUILayout.BeginHorizontal();

                        int tagPathCount = 0;
                        TagManager.TagPathCount.TryGetValue(tagPath, out tagPathCount);

                        if (this.tagCategory == TagCategory.All || (this.tagCategory == TagCategory.Used && tagPathCount > 0) || (this.tagCategory == TagCategory.Unused && tagPathCount == 0))
                        {
                            EditorGUI.BeginDisabledGroup(TagService.FixedTags.Any(f => f == tagPath));
                            GUIContent tagSelectedContent = new GUIContent(string.Format("({0}) {1}", tagPathCount, tagPath));
                            bool tagSelected = this.TagSelected[tagPath] = this.TagSelected.TryGetValue(tagPath, out tagSelected) && tagSelected;
                            this.TagSelected[tagPath] = EditorGUILayout.ToggleLeft(tagSelectedContent, tagSelected, tagHeaderStyle);
                            EditorGUI.EndDisabledGroup();
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUIUtility.labelWidth = 0;

                    EditorGUILayout.Space();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);

                    GUIContent tagAddContent = new GUIContent("Add", "Add tags to unity.");
                    if (GUILayout.Button(tagAddContent))
                    {
                        CreateTagPopup(tag => TagManager.AddTags(tag));
                        TagManager.RefreshTags();
                    }

                    GUIContent deleteGUIContent = new GUIContent("Delete");

                    if (GUILayout.Button(deleteGUIContent))
                    {
                        bool tagSelected = false;
                        foreach (IEnumerable<string> tagPath in this.findTagPaths.Where(p => this.TagSelected.TryGetValue(TagService.JoinTags(p), out tagSelected) && tagSelected))
                        {
                            TagManager.RemoveTags(tagPath.ToArray());
                            RefreshTags();
                        }
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndFadeGroup();
        }

        /// <summary>
        /// Draws the options.
        /// </summary>
        private void DrawOptions()
        {
            EditorGUILayout.BeginHorizontal();
            GUIContent optionsFoldoutContent = new GUIContent(string.Format("Options"));
            this.optionsFoldout.target = EditorGUILayout.Foldout(this.optionsFoldout.target, optionsFoldoutContent);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(string.Empty, CustomEditorStyles.HelpIconStyle))
            {
                Application.OpenURL("http://aiunity.com/products/multiple-tags/manual#gui-options");
            }
            EditorGUILayout.EndHorizontal();

            if (EditorGUILayout.BeginFadeGroup(this.optionsFoldout.faded))
            {
                EditorGUI.indentLevel++;

                // Create config internal level GUI
                Logger.InternalLogLevel = (LogLevels)EditorGUILayout.EnumFlagsField(this.internalLevelsContent, Logger.InternalLogLevel);

                GUIContent tagExpandContent = new GUIContent("Tag expand", "This will facilitate the generation of tag permutations." +
                    "  For example when selected the creation of tagPath T1/T2/T3 would also produce tagPaths TI and T1/T2." +
                    "  Also note with this option enabled the \"Tag optimize\" command will expand all tagPaths. The default is disabled.");
                TagManager.tagExpand = Convert.ToBoolean(EditorPrefs.GetInt("AiUnityMultipleTagsTagExpand", Convert.ToInt32(TagManager.tagExpand)));
                EditorPrefs.SetInt("AiUnityMultipleTagsTagExpand", Convert.ToInt32(EditorGUILayout.Toggle(tagExpandContent, TagManager.tagExpand)));

                GUIContent groupExpandContent = new GUIContent("Group expand", "This will facilitate the generation of group permutations." + "  For example when selected adding tag T1/Color.Red would automatically create T1/Color.Blue." + "  Also note with this option enabled the \"Tag optimize\" command will expand all tagGroups. The default is enabled.");
                TagManager.groupExpand = Convert.ToBoolean(EditorPrefs.GetInt("AiUnityMultipleTagsGroupExpand", Convert.ToInt32(TagManager.groupExpand)));
                EditorPrefs.SetInt("AiUnityMultipleTagsGroupExpand", Convert.ToInt32(EditorGUILayout.Toggle(groupExpandContent, TagManager.groupExpand)));
                EditorGUILayout.BeginHorizontal();

                GUIContent tagAccessCreatorContent = new GUIContent("TagAccess script", "Manually or automatically generate TagAccess.cs in folder AiUnity / UserData / MultipleTags / Resources." + "  TagAccess provides type safe access to Unity Tags and is utilized by the MultipleTags APIs." + "  With auto selected any change to the Unity tags will cause TagAccess.cs to regenerate.");

                EditorGUILayout.PrefixLabel(tagAccessCreatorContent);
                if (GUILayout.Button("Create", GUILayout.Width(50)))
                {
                    TagAccessCreator.Instance.Create();
                }

                EditorGUIUtility.labelWidth = 50;
                GUIContent autoUpdateContent = new GUIContent("Auto", "When selected any change to the Unity tags will cause TagAccess.cs to be regenerate.  The default is disabled.");
                bool autoUpdate = Convert.ToBoolean(EditorPrefs.GetInt("AiUnityMultipleTagsAutoUpdate", 0));
                EditorPrefs.SetInt("AiUnityMultipleTagsAutoUpdate", Convert.ToInt32(EditorGUILayout.Toggle(autoUpdateContent, autoUpdate)));
                EditorGUIUtility.labelWidth = 0;

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                GUIContent tagOptimizeContent = new GUIContent("Tag optimize", "Analyze and optimize all Unity tags.  This operation will irreversibly add and delete Unity tags." + "  Optimization includes consolidating and removing duplicate tagPaths. TagPaths and tagGroups are conditionally expanded based upon the expand options." + "  With auto selected any tag(s) that are added/removed in the GUI will be optimized.");
                EditorGUILayout.PrefixLabel(tagOptimizeContent);
                if (GUILayout.Button("Run", GUILayout.Width(50)))
                {
                    bool tagOptimizeConfirmation = EditorUtility.DisplayDialog("Confirm optimization", "This operation will irreversibly alter Unity tags.  Do you wish to proceed?", "OK", "Cancel");

                    if (tagOptimizeConfirmation)
                    {
                        TagManager.OptimizeTags();
                    }
                }

                EditorGUIUtility.labelWidth = 50;
                GUIContent autoOptimizeContent = new GUIContent("Auto", "When selected optimization is run on any tag(s) that are added or removed.  The default is enabled.");
                TagManager.AutoTagOptimize = Convert.ToBoolean(EditorPrefs.GetInt("AiUnityMultipleTagsAutoOptimize", 1));
                EditorPrefs.SetInt("AiUnityMultipleTagsAutoOptimize", Convert.ToInt32(EditorGUILayout.Toggle(autoOptimizeContent, TagManager.AutoTagOptimize)));
                EditorGUIUtility.labelWidth = 0;

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }

        /// <summary>
        /// Creates the GUI tags.
        /// </summary>
        /// <param name="activeTags">The active tags.</param>
        /// <param name="minusTagAction">The minus tag action.</param>
        /// <param name="addTagAction">The add tag action.</param>
        /// <param name="offset">The offset.</param>
        private void CreateGUITags(IEnumerable<string> activeTags, Action<string> minusTagAction = null, Action<string> addTagAction = null, int offset = 0)
        {
            IEnumerable<string> addTags = TagService.AllTags.Except(activeTags);

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.blue;
            labelStyle.margin.left = 8;

            // Calculations to wrap tags in editor window
            int editorWindowWidth = (int)EditorGUIUtility.currentViewWidth - offset - 24;
            var activeTagGroups = activeTags.Aggregate(new { acc = 0, d = new Dictionary<int, string>() }, (a, tag) =>
            {
                int width = (int)labelStyle.CalcSize(new GUIContent(tag)).x + 25;
                int overflow = Math.Max(0, width - ((a.acc + width) % editorWindowWidth));
                int total = a.acc + width + overflow; a.d.Add(total, tag);
                return new { acc = total, a.d };
            }).d.OrderBy(p => p.Key).GroupBy(x => (x.Key / editorWindowWidth));

            foreach (IGrouping<int, KeyValuePair<int, string>> activeTagGroup in activeTagGroups)
            {
                GUILayout.BeginHorizontal();

                foreach (string tag in activeTagGroup.Select(p => p.Value))
                {
                    Rect tagRect = GUILayoutUtility.GetRect(new GUIContent(tag), labelStyle, GUILayout.ExpandWidth(false));
                    Rect minusRect = GUILayoutUtility.GetRect(GUIContent.none, CustomEditorStyles.MinusIconMiniStyle, GUILayout.ExpandWidth(false));

                    Rect wrapperRect = new Rect(tagRect);
                    wrapperRect.x -= 2f;
                    wrapperRect.width += minusRect.width + 8f;

                    GUI.Box(wrapperRect, GUIContent.none, GUI.skin.button);

                    GUI.Label(tagRect, new GUIContent(tag), labelStyle);

                    if (GUI.Button(minusRect, GUIContent.none, CustomEditorStyles.MinusIconMiniStyle))
                    {
                        minusTagAction(tag);
                    }
                }

                if (addTagAction != null && activeTagGroups.Last().Key == activeTagGroup.Key)
                {
                    CreateTagAdditionMenu(addTags, addTagAction);
                }
                GUILayout.EndHorizontal();
            }

            if (addTagAction != null && !activeTagGroups.Any())
            {
                CreateTagAdditionMenu(addTags, addTagAction);
            }
        }

        /// <summary>
        /// Creates the tag addition menu.
        /// </summary>
        /// <param name="addTags">The add tags.</param>
        /// <param name="addTagAction">The add tag action.</param>
        private void CreateTagAdditionMenu(IEnumerable<string> addTags, Action<string> addTagAction)
        {
            Rect addTagRect = GUILayoutUtility.GetRect(CustomEditorStyles.PlusIconStyle.fixedWidth, CustomEditorStyles.PlusIconStyle.fixedHeight, CustomEditorStyles.PlusIconStyle, GUILayout.ExpandWidth(false));

            if (GUI.Button(addTagRect, GUIContent.none, CustomEditorStyles.PlusIconStyle))
            {
                GenericMenu targetMenu = CreateTagMenu(addTags, addTagAction);
                targetMenu.DropDown(addTagRect);
            }
        }

        /// <summary>
        /// Creates the tag popup.
        /// </summary>
        /// <param name="addTagAction">The add tag action.</param>
        private void CreateTagPopup(Action<string> addTagAction)
        {
            CreateTagWindow createTagWindow = ScriptableObject.CreateInstance<CreateTagWindow>();
            createTagWindow.Display((w, d) => { CreateTagItem(w, d, addTagAction); }, "Add Unity tag", position);
        }

        /// <summary>
        /// Creates the tag menu.
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="addTagAction">The add tag action.</param>
        private GenericMenu CreateTagMenu(IEnumerable<string> tags, Action<string> addTagAction)
        {
            GenericMenu tagMenu = new GenericMenu();

            tagMenu.AddItem(new GUIContent("New tag", "Add a tag"), false, () => CreateTagPopup(addTagAction));
            tagMenu.AddSeparator(string.Empty);

            foreach (string tag in tags)
            {
                string addTagClosure = tag;
                string expandTag = tag.Split('.').Aggregate(string.Empty, (p, s) => { string t = p.Split('/').Last(); return t == string.Empty ? s : t + "/" + t + "." + s; });
                tagMenu.AddItem(new GUIContent(expandTag, "Add a tag"), false, () => addTagAction(addTagClosure));
            }
            return tagMenu;
        }

        /// <summary>
        /// Adds the tag game objects.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="activeGameObjects">The active game objects.</param>
        private void AddTagGameObjects(string tag, params GameObject[] activeGameObjects)
        {
            TagManager.AddTags(activeGameObjects, tag);
            TagManager.RefreshTags();
        }

        /// <summary>
        /// Minuses the tag game objects.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="activeGameObjects">The active game objects.</param>
        private void MinusTagGameObjects(string tag, params GameObject[] activeGameObjects)
        {
            TagManager.RemoveTags(activeGameObjects, tag);
            TagManager.RefreshTags();
        }

        /// <summary>
        /// Creates the layout menu used to display available layouts.
        /// </summary>
        /// <param name="guiContent">Content of the GUI.</param>
        private GenericMenu CreateFindMenu(GUIContent guiContent)
        {
            GenericMenu findMenu = new GenericMenu();

            foreach (TagLogic tagLogic in EnumUtility.GetValues<TagLogic>().Where(l => l != TagLogic.None))
            {
                string menuItem = tagLogic.GetSymbol() + " = " + tagLogic.GetDescription();
                string findItem = string.Format("{0}", tagLogic.GetSymbol());
                findMenu.AddItem(new GUIContent(menuItem.Replace("&", "&&")), false, () => InsertFindText(guiContent, findItem));
            }
            findMenu.AddSeparator(string.Empty);

            foreach (RegexPattern tagRegexLogic in EnumUtility.GetValues<RegexPattern>())
            {
                string menuItem = tagRegexLogic.GetSymbol() + " = " + tagRegexLogic.GetDescription();
                string regexItem = string.Format("{0}", tagRegexLogic.GetSymbol());
                findMenu.AddItem(new GUIContent(menuItem), false, () => InsertFindText(guiContent, regexItem));
            }

            findMenu.AddSeparator(string.Empty);

            foreach (string tag in TagService.AllTags)
            {
                string findItem = string.Format("{0}", tag);
                findMenu.AddItem(new GUIContent(tag), false, () => InsertFindText(guiContent, findItem));
            }

            return findMenu;
        }

        /// <summary>
        /// Inserts the find text.
        /// </summary>
        /// <param name="guiContent">Content of the GUI.</param>
        /// <param name="findItem">The find item.</param>
        private void InsertFindText(GUIContent guiContent, string findItem)
        {
            try
            {
                TextEditor findExpressionEditor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;

                if (findExpressionEditor != null)
                {
                    guiContent.text = findExpressionEditor.text = guiContent.text.Insert(findExpressionEditor.cursorIndex, findItem);
                    findExpressionEditor.MoveWordRight();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unable to insert text into specified gui control.");
            }
        }
        #endregion
    }
}
