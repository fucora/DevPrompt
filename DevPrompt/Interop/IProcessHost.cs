﻿using System;
using System.Runtime.InteropServices;

namespace DevPrompt.Interop
{
    // MUST match DevPrompt.idl
    [Guid("cedddf4b-b229-4a17-8b10-140e53464efd")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IProcessHost
    {
        void Dispose();
        void Activate();
        void Deactivate();
        IntPtr GetWindow();
        void DpiChanged(double oldScale, double newScale);
        void Focus();

        [return: MarshalAs(UnmanagedType.Interface)]
        IProcess RunProcess(
            [MarshalAs(UnmanagedType.LPWStr)] string executable,
            [MarshalAs(UnmanagedType.LPWStr)] string arguments,
            [MarshalAs(UnmanagedType.LPWStr)] string startingDirectory);

        [return: MarshalAs(UnmanagedType.Interface)]
        IProcess RestoreProcess(
            [MarshalAs(UnmanagedType.LPWStr)] string executable,
            [MarshalAs(UnmanagedType.LPWStr)] string arguments,
            [MarshalAs(UnmanagedType.LPWStr)] string currentDirectory,
            [MarshalAs(UnmanagedType.LPWStr)] string environment,
            [MarshalAs(UnmanagedType.LPWStr)] string aliases,
            [MarshalAs(UnmanagedType.LPWStr)] string windowTitle);

        [return: MarshalAs(UnmanagedType.Interface)]
        IProcess CloneProcess([MarshalAs(UnmanagedType.Interface)] IProcess process);
    }
}
