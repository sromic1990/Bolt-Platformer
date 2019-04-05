// ***********************************************************************
// Assembly   : Assembly-CSharp
// Company    : AiUnity
// Author     : AiDesigner
//
// Created    : 08-13-2017
// Modified   : 08-28-2017
// ***********************************************************************
using System.Collections.Generic;

namespace AiUnity.MultipleTags.Examples
{
    /// <Summary>
    /// Provide strongly typed access to Unity tags.
    /// <Summary>
    public class TagAccessExample
    {
        public const string Untagged = "Untagged";
        public const string Respawn = "Respawn";
        public const string Finish = "Finish";
        public const string EditorOnly = "EditorOnly";
        public const string MainCamera = "MainCamera";
        public const string Player = "Player";
        public const string GameController = "GameController";
        public const string T2 = "T2";
        public const string T1 = "T1";
        public const string T3 = "T3";

        private static readonly List<string> tagPaths = new List<string>()
    {
        "Untagged",
        "Respawn",
        "Finish",
        "EditorOnly",
        "MainCamera",
        "Player",
        "GameController",
        "T2",
        "T3",
        "T1/T2",
        "T2/T3",
        "T1",
        "Color.Blue",
        "T3/Color.Red",
        "Color.Red/Color.Blue",
    };

        public IEnumerable<string> TagPaths { get { return tagPaths.AsReadOnly(); } }

        public class Color
        {
            public const string Red = "Color.Red";
            public const string Blue = "Color.Blue";

            public static string Any()
            {
                return "Color";
            }
        }
    }
}