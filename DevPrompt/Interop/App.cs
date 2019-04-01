﻿using System;
using System.Runtime.InteropServices;

namespace DevPrompt.Interop
{
    internal static class App
    {
        [DllImport("DevNative64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateApp")]
        private static extern void CreateApp64(IAppHost host, out IApp app);

        [DllImport("DevNative32", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateApp")]
        private static extern void CreateApp32(IAppHost host, out IApp app);

        public static IApp CreateApp(IAppHost host, out string errorMessage)
        {
            IApp app;

            try
            {
                if (Environment.Is64BitProcess)
                {
                    App.CreateApp64(host, out app);
                }
                else
                {
                    App.CreateApp32(host, out app);
                }

                errorMessage = null;
            }
            catch (TypeLoadException ex)
            {
                // Probably missing a DLL
                app = null;
                errorMessage = ex.Message;
            }

            return app;
        }
    }
}