using System.Drawing;
using System.Runtime.InteropServices;

namespace GameAutomation.Core.Services.Input;

/// <summary>
/// Input service implementation using Win32 API
/// </summary>
public class InputService : IInputService
{
    #region Win32 API

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;

    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    #endregion

    private static bool _dpiAwareSet = false;

    public InputService()
    {
        EnsureDPIAware();
    }

    private static void EnsureDPIAware()
    {
        if (!_dpiAwareSet)
        {
            SetProcessDPIAware();
            _dpiAwareSet = true;
        }
    }

    #region Mouse Operations

    public void MoveMouse(int x, int y)
    {
        SetCursorPos(x, y);
    }

    public Point GetMousePosition()
    {
        GetCursorPos(out POINT pt);
        return new Point(pt.X, pt.Y);
    }

    public void LeftClick()
    {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }

    public void RightClick()
    {
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
    }

    public void DoubleClick()
    {
        LeftClick();
        Thread.Sleep(50);
        LeftClick();
    }

    public void MouseDown()
    {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
    }

    public void MouseUp()
    {
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }

    public void MouseScroll(int amount)
    {
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)(amount * 120), 0);
    }

    #endregion

    #region Keyboard Operations

    public void KeyPress(VirtualKeyCode key)
    {
        keybd_event((byte)key, 0, KEYEVENTF_KEYDOWN, 0);
        keybd_event((byte)key, 0, KEYEVENTF_KEYUP, 0);
    }

    public void KeyPress(char character)
    {
        // Convert char to virtual key code
        short vk = VkKeyScan(character);
        byte virtualKey = (byte)(vk & 0xFF);
        bool shift = (vk & 0x100) != 0;

        if (shift)
            keybd_event((byte)VirtualKeyCode.SHIFT, 0, KEYEVENTF_KEYDOWN, 0);

        keybd_event(virtualKey, 0, KEYEVENTF_KEYDOWN, 0);
        keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, 0);

        if (shift)
            keybd_event((byte)VirtualKeyCode.SHIFT, 0, KEYEVENTF_KEYUP, 0);
    }

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    public void KeyDown(VirtualKeyCode key)
    {
        keybd_event((byte)key, 0, KEYEVENTF_KEYDOWN, 0);
    }

    public void KeyUp(VirtualKeyCode key)
    {
        keybd_event((byte)key, 0, KEYEVENTF_KEYUP, 0);
    }

    public void KeyCombination(params VirtualKeyCode[] keys)
    {
        // Press all keys down
        foreach (var key in keys)
            keybd_event((byte)key, 0, KEYEVENTF_KEYDOWN, 0);

        // Release all keys in reverse order
        for (int i = keys.Length - 1; i >= 0; i--)
            keybd_event((byte)keys[i], 0, KEYEVENTF_KEYUP, 0);
    }

    public void TypeText(string text)
    {
        foreach (char c in text)
        {
            KeyPress(c);
            Thread.Sleep(10);
        }
    }

    #endregion

    #region Async Operations

    public async Task ClickAsync(int x, int y, MouseButton button = MouseButton.Left)
    {
        await Task.Run(() =>
        {
            MoveMouse(x, y);
            Thread.Sleep(50);

            switch (button)
            {
                case MouseButton.Left:
                    LeftClick();
                    break;
                case MouseButton.Right:
                    RightClick();
                    break;
                case MouseButton.Middle:
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
                    mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
                    break;
            }
        });
    }

    public async Task MoveMouseAsync(int x, int y)
    {
        await Task.Run(() => MoveMouse(x, y));
    }

    public async Task PressKeyAsync(string key)
    {
        await Task.Run(() =>
        {
            if (Enum.TryParse<VirtualKeyCode>(key, true, out var vk))
                KeyPress(vk);
        });
    }

    public async Task TypeTextAsync(string text, int delayMs = 50)
    {
        await Task.Run(() =>
        {
            foreach (char c in text)
            {
                KeyPress(c);
                Thread.Sleep(delayMs);
            }
        });
    }

    public async Task PressKeyCombinationAsync(params string[] keys)
    {
        await Task.Run(() =>
        {
            var vkCodes = keys
                .Select(k => Enum.TryParse<VirtualKeyCode>(k, true, out var vk) ? vk : (VirtualKeyCode?)null)
                .Where(vk => vk.HasValue)
                .Select(vk => vk!.Value)
                .ToArray();

            KeyCombination(vkCodes);
        });
    }

    #endregion
}
