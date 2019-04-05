// ***********************************************************************
// Assembly   : Assembly-CSharp-Editor
// Company    : AiUnity
// Author     : AiDesigner
//
// Created    : 07-07-2017
// Modified   : 01-11-2018
// ***********************************************************************
using AiUnity.Common.Attributes;
using System.ComponentModel;

namespace AiUnity.MultipleTags.Editor
{
    /// <summary>
    /// Enum to express common Regex syntax for the GUI.
    /// Note any valid regex expression is allowed in GUI expressions.
    /// </summary>
    public enum RegexPattern
    {
        [EnumSymbol(".*")]
        [Description("0 or more of any character")]
        WildAsterisk = 1,

        [EnumSymbol(".+")]
        [Description("1 or more of any character")]
        WildPlus,

        [EnumSymbol(".?")]
        [Description("0 or 1 more of any character")]
        WildQuestionMark,

        [EnumSymbol("[ ]")]
        [Description("character group")]
        CharacterGroup
    }
}
