// ***********************************************************************
// Assembly         : Assembly-CSharp
// Author           : AiDesigner
// Created          : 06-20-2016
// Modified         : 07-23-2018
// ***********************************************************************
#if AIUNITY_CODE

using System;

namespace AiUnity.Common.Log
{
    /// <summary>
    /// Interface for Game Console
    /// </summary>
    public interface IGameConsoleController
    {
        #region Methods
        /// <summary>
        /// Adds the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="logLevels">The log levels.</param>
        /// <param name="loggerName">Name of the logger.</param>
        /// <param name="timeStamp">The time stamp.</param>
        void AddMessage(int logLevels, string message, string loggerName = null, DateTime dateTime = default(DateTime));

        /// <summary>
        /// Sets the console active.
        /// </summary>
        /// <param name="consoleActive">if set to <c>true</c> [console active].</param>
        void SetConsoleActive(bool consoleActive);

        /// <summary>
        /// Sets the size of the font.
        /// </summary>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="updateControl">if set to <c>true</c> [update control].</param>
        /// <param name="updateMessage">if set to <c>true</c> [update message].</param>
        void SetFontSize(int fontSize, bool updateControl = true, bool updateMessage = true);

        /// <summary>
        /// Sets the icon enable.
        /// </summary>
        /// <param name="iconEnable">if set to <c>true</c> [icon enable].</param>
        void SetIconEnable(bool iconEnable);

        /// <summary>
        /// Sets the log level filter.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="updateControl">if set to <c>true</c> [update control].</param>
        /// <param name="updateMessage">if set to <c>true</c> [update message].</param>
        void SetLogLevelFilter(LogLevels level, bool updateControl = true, bool updateMessage = true);
        #endregion
    }
}
#endif