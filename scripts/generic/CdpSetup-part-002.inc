        KeybdEvent(CdpWin32.VkRight, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkControl, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkLWin, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        Thread.Sleep(CdpTimeout.ForegroundDelayMs);
    }

    private static void SwitchDesktopLeft()
    {
        KeybdEvent(CdpWin32.VkLWin, 0, 0, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkControl, 0, 0, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkLeft, 0, 0, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkLeft, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkControl, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkLWin, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        Thread.Sleep(CdpTimeout.ForegroundDelayMs);
    }

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint SendInput(uint Count, Input[] Inputs, int Size);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public Keybdinput Ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Keybdinput
    {
        public ushort Vk;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public IntPtr Extra;
    }

    private static void TypeString(string Text)
    {
        foreach (var Ch in Text)
        {
            var Inputs = new Input[2];
            Inputs[0] = new Input { Type = 1, U = new InputUnion { Ki = new Keybdinput { Vk = 0, Scan = Ch, Flags = 4 } } };
            Inputs[1] = new Input { Type = 1, U = new InputUnion { Ki = new Keybdinput { Vk = 0, Scan = Ch, Flags = 4 | 2 } } };
            SendInput(2, Inputs, Marshal.SizeOf<Input>());
        }
    }

    private static void NavigateAddressBar(string Url)
    {
        var Root = AutomationElement.RootElement;
        var ChromeCondition = new PropertyCondition(AutomationElement.ClassNameProperty, CdpProto.ChromeWidgetClass);
        var Window = Root.FindFirst(TreeScope.Children, ChromeCondition);
        if (Window == null)
        {
            Console.Error.WriteLine("Chrome not found");
            return;
        }

        SwitchToWindow(new IntPtr(Window.Current.NativeWindowHandle));
        Thread.Sleep(200);
        var Thread2 = new Thread(() => System.Windows.Forms.Clipboard.SetText(Url));
        Thread2.SetApartmentState(ApartmentState.STA);
        Thread2.Start();
        Thread2.Join();
        KeybdEvent(CdpWin32.VkEscape, 0, 0, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkEscape, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        Thread.Sleep(100);
        KeybdEvent(CdpWin32.VkControl, 0, 0, UIntPtr.Zero);
        KeybdEvent(0x4C, 0, 0, UIntPtr.Zero);
        KeybdEvent(0x4C, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkControl, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        Thread.Sleep(300);
        KeybdEvent(CdpWin32.VkControl, 0, 0, UIntPtr.Zero);
        KeybdEvent(0x56, 0, 0, UIntPtr.Zero);
        KeybdEvent(0x56, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkControl, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        Thread.Sleep(200);
        KeybdEvent(CdpWin32.VkReturn, 0, 0, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkReturn, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        Console.WriteLine($"Navigated to {Url}");
    }

    private static void FocusChrome()
    {
        var Root = AutomationElement.RootElement;
        var ChromeCondition = new PropertyCondition(AutomationElement.ClassNameProperty, CdpProto.ChromeWidgetClass);
        var Window = Root.FindFirst(TreeScope.Children, ChromeCondition);
        if (Window != null)
        {
            SwitchToWindow(new IntPtr(Window.Current.NativeWindowHandle));
            Thread.Sleep(300);
            DismissInfobar();
            Console.WriteLine("Chrome focused");
            return;
        }

        SwitchDesktopRight();
        Window = AutomationElement.RootElement.FindFirst(TreeScope.Children, ChromeCondition);
        if (Window != null)
        {
            SwitchToWindow(new IntPtr(Window.Current.NativeWindowHandle));
            Thread.Sleep(300);
            DismissInfobar();
            Console.WriteLine("Chrome focused");
            return;
        }

        SwitchDesktopLeft();
        SwitchDesktopLeft();
        Window = AutomationElement.RootElement.FindFirst(TreeScope.Children, ChromeCondition);
        if (Window != null)
        {
            SwitchToWindow(new IntPtr(Window.Current.NativeWindowHandle));
            Console.WriteLine("Chrome focused");
        }
        else
        {
            Console.Error.WriteLine("Chrome not found");
        }
    }

    private static (string Command, Dictionary<string, object> Args) ParseArgs(string[] Argv)
    {
        var Command = Argv[0];
        var Result = new Dictionary<string, object>(StringComparer.Ordinal);
        var Index = 1;
        while (Index < Argv.Length)
        {
            if (Argv[Index].StartsWith(CdpArg.ArgPrefix, StringComparison.Ordinal))
            {
                var Key = Argv[Index][2..];
                if (Key.StartsWith(CdpArg.NoPrefix, StringComparison.Ordinal))
                {
                    Result[Key[3..]] = false;
                    Index++;
                    continue;
                }

                if (Index + 1 < Argv.Length && !Argv[Index + 1].StartsWith(CdpArg.ArgPrefix, StringComparison.Ordinal))
                {
                    var Value = Argv[Index + 1];
                    Index += 2;
                    if (bool.TryParse(Value, out var BoolValue))
                    {
                        Result[Key] = BoolValue;
                    }
                    else if (int.TryParse(Value, System.Globalization.CultureInfo.InvariantCulture, out var IntValue))
                    {
                        Result[Key] = IntValue;
                    }
                    else if (double.TryParse(Value, System.Globalization.CultureInfo.InvariantCulture, out var DoubleValue))
                    {
                        Result[Key] = DoubleValue;
                    }
                    else
                    {
                        Result[Key] = Value;
                    }
                }
                else
                {
                    Result[Key] = true;
                    Index++;
                }
            }
            else
            {
                switch (Command)
                {
                    case "click" or "hover":
                        if (!Result.ContainsKey(CdpArg.Uid))
                        {
                            Result[CdpArg.Uid] = Argv[Index];
                        }

                        break;
                    case "fill":
                        if (!Result.ContainsKey(CdpArg.Uid))
                        {
                            Result[CdpArg.Uid] = Argv[Index];
                        }
                        else if (!Result.ContainsKey(CdpKey.Value))
                        {
                            Result[CdpKey.Value] = Argv[Index];
                        }

                        break;
                    case "select_page" or "close_page":
                        if (!Result.ContainsKey(CdpArg.PageId))
                        {
                            Result[CdpArg.PageId] = int.Parse(Argv[Index], System.Globalization.CultureInfo.InvariantCulture);
                        }

                        break;
                    case "new_page":
                        if (!Result.ContainsKey(CdpKey.Url))
                        {
                            Result[CdpKey.Url] = Argv[Index];
                        }

                        break;
                    case "evaluate_script":
                        if (!Result.ContainsKey(CdpArg.Function))
                        {
                            Result[CdpArg.Function] = Argv[Index];
                        }

                        break;
                    case "press_key":
                        if (!Result.ContainsKey(CdpKey.Key))
                        {
                            Result[CdpKey.Key] = Argv[Index];
                        }

                        break;
                    case "type_text":
                        if (!Result.ContainsKey(CdpKey.Text))
                        {
                            Result[CdpKey.Text] = Argv[Index];
                        }

                        break;
                    case "resize_page":
                        if (!Result.ContainsKey(CdpKey.Width))
                        {
                            Result[CdpKey.Width] = int.Parse(Argv[Index], System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else if (!Result.ContainsKey(CdpKey.Height))
                        {
                            Result[CdpKey.Height] = int.Parse(Argv[Index], System.Globalization.CultureInfo.InvariantCulture);
                        }

                        break;
                    case "handle_dialog":
                        if (!Result.ContainsKey(CdpArg.Action))
                        {
                            Result[CdpArg.Action] = Argv[Index];
                        }

                        break;
                    case "drag":
                        if (!Result.ContainsKey(CdpArg.FromUid))
                        {
                            Result[CdpArg.FromUid] = Argv[Index];
                        }
                        else if (!Result.ContainsKey(CdpArg.ToUid))
                        {
                            Result[CdpArg.ToUid] = Argv[Index];
                        }

                        break;
                }

                Index++;
            }
        }

        return (Command, Result);
    }

    private static void PrintHelp()
    {
        Console.Write("chrome-devtools.cs - Pure .NET 11 Chrome DevTools CLI\n");
        Console.Write("\n");
        Console.Write("Usage: dotnet run -- <command> [args] [--options]\n");
        Console.Write("\n");
        Console.Write("Connects via raw CDP WebSocket. Auto-clicks Allow prompt (DPI-aware).\n");
