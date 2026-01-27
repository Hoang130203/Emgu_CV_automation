using System.Drawing;

namespace GameAutomation.Core.Services.Input;

/// <summary>
/// Interface for simulating keyboard and mouse input
/// Sử dụng InputSimulatorCore library
/// </summary>
public interface IInputService
{
    // === Mouse Operations ===

    /// <summary>
    /// Di chuyển chuột tới vị trí (x, y)
    /// </summary>
    void MoveMouse(int x, int y);

    /// <summary>
    /// Lấy vị trí hiện tại của chuột
    /// </summary>
    Point GetMousePosition();

    /// <summary>
    /// Click chuột trái
    /// </summary>
    void LeftClick();

    /// <summary>
    /// Click chuột phải
    /// </summary>
    void RightClick();

    /// <summary>
    /// Double click chuột trái
    /// </summary>
    void DoubleClick();

    /// <summary>
    /// Giữ chuột trái (mouse down)
    /// </summary>
    void MouseDown();

    /// <summary>
    /// Thả chuột trái (mouse up)
    /// </summary>
    void MouseUp();

    /// <summary>
    /// Scroll chuột (positive = scroll up, negative = scroll down)
    /// </summary>
    void MouseScroll(int amount);

    // === Keyboard Operations ===

    /// <summary>
    /// Nhấn phím (VirtualKeyCode hoặc char)
    /// </summary>
    void KeyPress(VirtualKeyCode key);

    /// <summary>
    /// Nhấn phím (character)
    /// </summary>
    void KeyPress(char character);

    /// <summary>
    /// Giữ phím (key down)
    /// </summary>
    void KeyDown(VirtualKeyCode key);

    /// <summary>
    /// Thả phím (key up)
    /// </summary>
    void KeyUp(VirtualKeyCode key);

    /// <summary>
    /// Nhấn tổ hợp phím (Ctrl+C, Alt+Tab, etc.)
    /// </summary>
    void KeyCombination(params VirtualKeyCode[] keys);

    /// <summary>
    /// Nhập text (string)
    /// </summary>
    void TypeText(string text);

    // === Async versions (for compatibility) ===

    Task ClickAsync(int x, int y, MouseButton button = MouseButton.Left);
    Task MoveMouseAsync(int x, int y);
    Task PressKeyAsync(string key);
    Task TypeTextAsync(string text, int delayMs = 50);
    Task PressKeyCombinationAsync(params string[] keys);
}

public enum MouseButton
{
    Left,
    Right,
    Middle
}

/// <summary>
/// Virtual Key Codes (Windows Virtual-Key Codes)
/// Tương thích với InputSimulatorCore
/// </summary>
public enum VirtualKeyCode
{
    // Modifier keys
    SHIFT = 0x10,
    CONTROL = 0x11,
    ALT = 0x12,
    LSHIFT = 0xA0,
    RSHIFT = 0xA1,
    LCONTROL = 0xA2,
    RCONTROL = 0xA3,
    LALT = 0xA4,
    RALT = 0xA5,

    // Function keys
    F1 = 0x70,
    F2 = 0x71,
    F3 = 0x72,
    F4 = 0x73,
    F5 = 0x74,
    F6 = 0x75,
    F7 = 0x76,
    F8 = 0x77,
    F9 = 0x78,
    F10 = 0x79,
    F11 = 0x7A,
    F12 = 0x7B,

    // Navigation keys
    LEFT = 0x25,
    UP = 0x26,
    RIGHT = 0x27,
    DOWN = 0x28,
    PRIOR = 0x21,      // Page Up
    NEXT = 0x22,       // Page Down
    HOME = 0x24,
    END = 0x23,
    INSERT = 0x2D,
    DELETE = 0x2E,

    // Special keys
    RETURN = 0x0D,     // Enter
    ESCAPE = 0x1B,     // Esc
    SPACE = 0x20,
    TAB = 0x09,
    BACK = 0x08,       // Backspace
    CAPITAL = 0x14,    // Caps Lock

    // Number keys
    VK_0 = 0x30,
    VK_1 = 0x31,
    VK_2 = 0x32,
    VK_3 = 0x33,
    VK_4 = 0x34,
    VK_5 = 0x35,
    VK_6 = 0x36,
    VK_7 = 0x37,
    VK_8 = 0x38,
    VK_9 = 0x39,

    // Letter keys
    A = 0x41,
    B = 0x42,
    C = 0x43,
    D = 0x44,
    E = 0x45,
    F = 0x46,
    G = 0x47,
    H = 0x48,
    I = 0x49,
    J = 0x4A,
    K = 0x4B,
    L = 0x4C,
    M = 0x4D,
    N = 0x4E,
    O = 0x4F,
    P = 0x50,
    Q = 0x51,
    R = 0x52,
    S = 0x53,
    T = 0x54,
    U = 0x55,
    V = 0x56,
    W = 0x57,
    X = 0x58,
    Y = 0x59,
    Z = 0x5A,

    // Numpad keys
    NUMPAD0 = 0x60,
    NUMPAD1 = 0x61,
    NUMPAD2 = 0x62,
    NUMPAD3 = 0x63,
    NUMPAD4 = 0x64,
    NUMPAD5 = 0x65,
    NUMPAD6 = 0x66,
    NUMPAD7 = 0x67,
    NUMPAD8 = 0x68,
    NUMPAD9 = 0x69,
    MULTIPLY = 0x6A,
    ADD = 0x6B,
    SEPARATOR = 0x6C,
    SUBTRACT = 0x6D,
    DECIMAL = 0x6E,
    DIVIDE = 0x6F,

    // Other keys
    SNAPSHOT = 0x2C,   // Print Screen
    PAUSE = 0x13,
    SCROLL = 0x91,     // Scroll Lock
    NUMLOCK = 0x90
}
