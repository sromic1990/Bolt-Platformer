// ***********************************************************************
// Assembly         : Assembly-CSharp-Editor
// Author           : AiDesigner
// Created          : 07-09-2016
// Modified         : 06-16-2017
// ***********************************************************************
using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace AiUnity.MultipleTags.Editor
{
    /// <summary>
    /// Holds data regarding context menu creation.
    /// </summary>
    /// <seealso cref="UnityEngine.ScriptableObject" />
    [Serializable]
    public class CreateTagData : ScriptableObject
    {
        #region Properties
        /// <summary>

        /// <summary>
        /// Gets or sets the name of the menu.
        /// </summary>
        public string Tags { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MenuEntryData"/> class.
        /// </summary>
        /// <param name="menuPath">The menu path.</param>
        /// <param name="menuName">Name of the menu.</param>
        public CreateTagData(string tags)
        {
            Tags = tags;
        }
        #endregion
    }
}