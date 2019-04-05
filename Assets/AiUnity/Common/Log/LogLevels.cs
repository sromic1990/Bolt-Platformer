// ***********************************************************************
// Assembly   : Assembly-CSharp
// Company    : AiUnity
// Author     : AiDesigner
//
// Created    : 06-20-2016
// Modified   : 07-23-2018
// ***********************************************************************
#if AIUNITY_CODE

namespace AiUnity.Common.Log
{
    using System;

    /// <summary>
    /// Log Levels
    /// </summary>
    [Flags]
    public enum LogLevels
    {
        /// <summary> Trace logging level </summary>
        Trace = 64,

        /// <summary> Debug logging level </summary>
        Debug = 32,

        /// <summary> Info logging level </summary>
        Info = 16,

        /// <summary> Warn logging level </summary>
        Warn = 8,

        /// <summary> Error logging level </summary>
        Error = 4,

        /// <summary> Fatal logging level </summary>
        Fatal = 2,

        /// <summary> Assert logging level </summary>
        Assert = 1,

        /// <summary> All logging level </summary>
        Everything = -1
    }
}
#endif