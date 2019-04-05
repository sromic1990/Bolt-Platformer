using System;
using AiUnity.Common.Attributes;
using System.ComponentModel;

namespace AiUnity.MultipleTags.Core
{
    // Use this for initialization
    [Flags]
    public enum TagLogic
    {
        None = 0,

        [EnumSymbol("!")]
        [Description("Inverter")]
        Invert = 1 << 0,

        [EnumSymbol("&")]
        [Description("And")]
        And = 1 << 1,

        [EnumSymbol("|")]
        [Description("Or")]
        Or = 1 << 2,

        [EnumSymbol("^")]
        [Description("Xor")]
        Xor = 1 << 3,

        [EnumSymbol("( )")]
        [Description("Expression group")]
        Expression = 1 << 4
    }
}
