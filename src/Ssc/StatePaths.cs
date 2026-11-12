// SPDX-FileCopyrightText: Copyright 2026 Robert Bopko
// SPDX-License-Identifier: BSD-2-Clause

namespace Ssc;

public sealed class StatePaths(string root)
{
    public string Root { get; } = root;
    public string TokenFile => Path.Combine(Root, "refresh_token");
    public string QrPng => Path.Combine(Root, "qr.png");
    public string QrHtml => Path.Combine(Root, "qr.html");

    public static StatePaths Default() =>
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ssc"));

    public static string? WindowsPathForWsl(string linuxPath, string? distroName) =>
        string.IsNullOrEmpty(distroName) ? null : $@"\\wsl.localhost\{distroName}{linuxPath.Replace('/', '\\')}";
}
