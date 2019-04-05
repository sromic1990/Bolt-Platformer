// ***********************************************************************
// Assembly         : Assembly-CSharp
// Author           : AiDesigner
// Created          : 06-22-2016
// Modified         : 07-23-2018
// ***********************************************************************
#if AIUNITY_CODE

using System;

namespace AiUnity.Common.Log
{
    /// <summary>
    /// Interface Logger must implement
    /// </summary>
    public interface ILogger
    {
        #region Properties
        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Logs the specified level.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        void Log(LogLevels level, UnityEngine.Object context, Exception exception, string message, params object[] args);
        #endregion
    }
}
#endif