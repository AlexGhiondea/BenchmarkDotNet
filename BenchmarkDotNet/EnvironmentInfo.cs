﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Reflection;

namespace BenchmarkDotNet
{
    public sealed class EnvironmentInfo
    {
        static EnvironmentInfo()
        {
            MainCultureInfo = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            MainCultureInfo.NumberFormat.NumberDecimalSeparator = ".";
        }

        public static readonly CultureInfo MainCultureInfo;

        public string BenchmarkDotNetCaption { get; set; }
        public string BenchmarkDotNetVersion { get; set; }
        public string OsVersion { get; set; }
        public string ProcessorName { get; set; }
        public int ProcessorCount { get; set; }
        public string ClrVersion { get; set; }
        public string Architecture { get; set; }
        public bool HasAttachedDebugger { get; set; }
        public bool HasRyuJit { get; set; }
        public string Configuration { get; set; }

        public static EnvironmentInfo GetCurrentInfo() => new EnvironmentInfo
        {
            BenchmarkDotNetCaption = GetBenchmarkDotNetCaption(),
            BenchmarkDotNetVersion = GetBenchmarkDotNetVersion(),
            OsVersion = GetOsVersion(),
            ProcessorName = GetProcessorName(),
            ProcessorCount = GetProcessorCount(),
            ClrVersion = GetClrVersion(),
            Architecture = GetArchitecture(),
            HasAttachedDebugger = GetHasAttachedDebugger(),
            HasRyuJit = GetHasRyuJit(),
            Configuration = GetConfiguration()
        };

        public string ToFormattedString(string clrHint = "", bool printDoubleSlash = true)
        {
            var prefix = printDoubleSlash ? "// " : string.Empty;
            var line1 = $"{prefix}{BenchmarkDotNetCaption}=v{BenchmarkDotNetVersion}";
            var line2 = $"{prefix}OS={OsVersion}";
            var line3 = $"{prefix}Processor={ProcessorName}, ProcessorCount={ProcessorCount}";
            var line4 = $"{prefix}{clrHint}CLR={ClrVersion}, Arch={Architecture} {Configuration}{GetDebuggerFlag()}{GetJitFlag()}";
            return string.Join(System.Environment.NewLine, line1, line2, line3, line4);
        }

        private string GetJitFlag() => HasRyuJit ? " [RyuJIT]" : "";
        private string GetDebuggerFlag() => HasAttachedDebugger ? " [AttachedDebugger]" : "";

        private static string GetBenchmarkDotNetCaption() => 
            ((AssemblyTitleAttribute)typeof(BenchmarkRunner).Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title;

        private static string GetBenchmarkDotNetVersion() => 
            typeof(BenchmarkRunner).Assembly.GetName().Version + (GetBenchmarkDotNetCaption().EndsWith("-Dev") ? "+" : string.Empty);

        private static string GetOsVersion() => Environment.OSVersion.ToString();

        private static string GetProcessorName()
        {
            var info = string.Empty;
            if (IsWindows() && !IsMono())
            {
                try
                {
                    var mosProcessor = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                    foreach (var moProcessor in mosProcessor.Get().Cast<ManagementObject>())
                        info += moProcessor["name"]?.ToString();
                }
                catch (Exception)
                {
                }
            }
            else
                info = "?";
            return info;
        }

        private static int GetProcessorCount() => Environment.ProcessorCount;

        private static string GetClrVersion()
        {
            if (IsMono())
            {
                var monoRuntimeType = Type.GetType("Mono.Runtime");
                var monoDisplayName = monoRuntimeType?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (monoDisplayName != null)
                    return "Mono " + monoDisplayName.Invoke(null, null);
            }
            return "MS.NET " + Environment.Version;
        }

        private static string GetArchitecture() => IntPtr.Size == 4 ? "32-bit" : "64-bit";

        private static bool GetHasAttachedDebugger() => Debugger.IsAttached;

        private static bool GetHasRyuJit()
        {
            if (Type.GetType("Mono.Runtime") == null && IntPtr.Size == 8 && GetConfiguration() != "DEBUG")
                if (!new JitHelper().IsMsX64())
                    return true;
            return false;
        }

        private static string GetConfiguration()
        {
            string configuration = "";
#if DEBUG
            configuration = "DEBUG";
#endif
            return configuration;
        }

        private static bool IsMono() => Type.GetType("Mono.Runtime") != null;

        private static bool IsWindows() => 
            new[] {PlatformID.Win32NT, PlatformID.Win32S, PlatformID.Win32Windows, PlatformID.WinCE}.Contains(Environment.OSVersion.Platform);

        // See http://aakinshin.net/en/blog/dotnet/jit-version-determining-in-runtime/
        private class JitHelper
        {
            private int bar;

            public bool IsMsX64(int step = 1)
            {
                var value = 0;
                for (int i = 0; i < step; i++)
                {
                    bar = i + 10;
                    for (int j = 0; j < 2 * step; j += step)
                        value = j + 10;
                }
                return value == 20 + step;
            }
        }
    }
}