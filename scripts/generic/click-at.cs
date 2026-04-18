#:property TargetFramework=net11.0-windows
#:property AllowUnsafeBlocks=true
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false

using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/click-at.cs scripts/<click-at-X>-config.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Nums = ClickAtPatterns.ConstInt().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => int.Parse(M.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);

if (!Nums.TryGetValue("X", out var X)) { await Console.Error.WriteLineAsync("specific must declare const int X"); return 3; }
if (!Nums.TryGetValue("Y", out var Y)) { await Console.Error.WriteLineAsync("specific must declare const int Y"); return 4; }

var Sent = ClickAtNative.ClickAtPoint(X, Y);
await Task.Delay(80);
return Sent == 3 ? 0 : 5;

namespace Scripts
{
    internal static partial class ClickAtPatterns
    {
        [GeneratedRegex(@"const\s+int\s+(?<name>\w+)\s*=\s*(?<value>-?\d+)\s*;", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstInt();
    }

    internal static class ClickAtNative
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int GetSystemMetrics(int nIndex);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct INPUT { public uint type; public InputUnion U; }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        private struct InputUnion { [System.Runtime.InteropServices.FieldOffset(0)] public MOUSEINPUT mi; }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

        public static uint ClickAtPoint(int X, int Y)
        {
            const int SM_CXSCREEN = 0;
            const int SM_CYSCREEN = 1;
            const uint INPUT_MOUSE = 0;
            const uint MOUSEEVENTF_MOVE = 0x0001;
            const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
            const uint MOUSEEVENTF_LEFTUP = 0x0004;
            const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

            var W = GetSystemMetrics(SM_CXSCREEN);
            var H = GetSystemMetrics(SM_CYSCREEN);
            var AbsX = (int)((X * 65535.0) / W);
            var AbsY = (int)((Y * 65535.0) / H);
            SetCursorPos(X, Y);
            var Inputs = new INPUT[]
            {
                new() { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = AbsX, dy = AbsY, dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE } } },
                new() { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = AbsX, dy = AbsY, dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE } } },
                new() { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = AbsX, dy = AbsY, dwFlags = MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE } } },
            };
            return SendInput((uint)Inputs.Length, Inputs, System.Runtime.InteropServices.Marshal.SizeOf<INPUT>());
        }
    }
}
