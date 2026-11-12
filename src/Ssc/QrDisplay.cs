// SPDX-FileCopyrightText: Copyright 2026 Robert Bopko
// SPDX-License-Identifier: BSD-2-Clause

using QRCoder;

namespace Ssc;

public sealed class QrDisplay(StatePaths paths, TextWriter output)
{
    public void Show(string challengeUrl)
    {
        using var data = new QRCodeGenerator().CreateQrCode(challengeUrl, QRCodeGenerator.ECCLevel.M);
        output.WriteLine(new AsciiQRCode(data).GetGraphic(1, drawQuietZones: true));

        Directory.CreateDirectory(paths.Root);
        File.WriteAllBytes(paths.QrPng, new PngByteQRCode(data).GetGraphic(10));
        File.WriteAllText(paths.QrHtml, $"<meta http-equiv=refresh content=3><img src=\"qr.png?{DateTime.UtcNow.Ticks}\">");

        output.WriteLine("Scan the QR code with the Steam mobile app.");
        output.WriteLine($"Or open: {paths.QrHtml}");
        var windowsPath = StatePaths.WindowsPathForWsl(paths.QrHtml, Environment.GetEnvironmentVariable("WSL_DISTRO_NAME"));
        if (windowsPath is not null)
        {
            output.WriteLine($"     or: {windowsPath}");
        }
    }

    public void Clear()
    {
        File.Delete(paths.QrPng);
        File.Delete(paths.QrHtml);
    }
}
