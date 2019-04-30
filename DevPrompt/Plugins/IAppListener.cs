﻿namespace DevPrompt.Plugins
{
    /// <summary>
    /// Plugins implement this to listen to global app events
    /// </summary>
    public interface IAppListener
    {
        void OnStartup(IApp app);
        void OnExit(IApp app);
    }
}