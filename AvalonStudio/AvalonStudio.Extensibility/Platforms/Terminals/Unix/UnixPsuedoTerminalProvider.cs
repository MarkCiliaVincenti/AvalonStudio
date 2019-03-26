﻿using AvalonStudio.Platforms.Terminals;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AvalonStudio.Extensibility.Platforms.Terminals.Unix
{
    [Shared]
    [Export(typeof(IExtension))]
    [Export(typeof(IPsuedoTerminalProvider))]
    class UnixPsuedoTerminalProvider : IPsuedoTerminalProvider, IExtension
    {
        public IPsuedoTerminal Create(int columns, int rows, string initialDirectory, string environment, string command, params string[] arguments)
        {
            var fdm = Native.open("/dev/ptmx", Native.O_RDWR | Native.O_NOCTTY);

            var res = Native.grantpt(fdm);
            res = Native.unlockpt(fdm);

            var namePtr = Native.ptsname(fdm);
            var name = Marshal.PtrToStringAnsi(namePtr);
            var fds = Native.open(name, (int)Native.O_RDWR);

            var fileActions = Marshal.AllocHGlobal(1024);
            Native.posix_spawn_file_actions_init(fileActions);
            res = Native.posix_spawn_file_actions_adddup2(fileActions, (int)fds, 0);
            res = Native.posix_spawn_file_actions_adddup2(fileActions, (int)fds, 1);
            res = Native.posix_spawn_file_actions_adddup2(fileActions, (int)fds, 2);
            res = Native.posix_spawn_file_actions_addclose(fileActions, (int)fdm);
            res = Native.posix_spawn_file_actions_addclose(fileActions, (int)fds);


            var attributes = Marshal.AllocHGlobal(1024);
            res = Native.posix_spawnattr_init(attributes);

            res = Native.posix_spawnp(out var pid, "dotnet", fileActions, attributes, new string[] { "dotnet", "/home/ubuntu/repos/AvalonStudio/AvalonStudio/AvalonStudio/bin/Debug/netcoreapp2.2/AvalonStudio.dll", "--trampoline", null }, new string[] { "TERM=xterm-256color", null });

            var fs = new FileStream(new SafeFileHandle(fdm, true), FileAccess.ReadWrite);
            var process = Process.GetProcessById((int)pid);
            return new UnixPsuedoTerminal(process, fds, fdm, fs, fs);
        }
    }
}
