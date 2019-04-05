// ***********************************************************************
// Assembly         : Assembly-CSharp-Editor
// Author           : AiDesigner
// Created          : 07-09-2016
// Modified         : 07-25-2017
// ***********************************************************************
using AiUnity.Common.Editor.ModalWindow;
using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using AiUnity.Common.Editor.Styles;

namespace AiUnity.MultipleTags.Editor
{
    /// <summary>
    /// Create menu entry modal window to get MenuItemData.
    /// </summary>
    /// <seealso cref="AiUnity.Common.Editor.ModalWindow.ModalWindow{AiUnity.ScriptBuilder.Editor.MenuEntryData}" />
    public class CreateTagWindow : ModalWindow<CreateTagData>
    {
        #region Properties
        /// <summary>
        /// Gets the height.
        /// </summary>
        protected override float Height
        {
            get { return 120; }
        }

        /// <summary>
        /// Gets the width.
        /// </summary>
        protected override float Width
        {
            get { return 400; }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Draws the specified region.
        /// </summary>
        /// <param name="region">The region.</param>
        protected override void Draw(Rect region)
        {
            EditorGUIUtility.labelWidth = 100f;

            EditorGUILayout.BeginVertical("box");

            GUIContent tagContent = new GUIContent("New tag(s)", "Enter new tag(s) separated by spaces.");
            Data.Tags = EditorGUILayout.TextField(tagContent, Data.Tags);
            EditorGUILayout.HelpBox("Add tag(s) to Unity using a space delimiter.  For a gameObject to have tags T1 and T2 you would create tag \"T1/T2\".", MessageType.Info);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Ok", GUILayout.ExpandWidth(false)))
            {
                Ok();
                Close();
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(false)))
            {
                Cancel();
                Close();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Called when [enable].
        /// </summary>
        private void OnEnable()
        {
            Data = ScriptableObject.CreateInstance<CreateTagData>();
        }
        #endregion
    }
}