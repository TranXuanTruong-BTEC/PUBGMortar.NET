using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PUBGMortar.Views;

/// <summary>
/// Cửa sổ thông báo nổi - hỗ trợ chuột xuyên qua
/// </summary>
public partial class OverlayWindow : Window
{
    #region Win32 API for Click-Through

    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;  // Không hiện trên taskbar và Alt+Tab
    private const int GWL_EXSTYLE = -20;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    #endregion

    public OverlayWindow(string message)
    {
        InitializeComponent();
        MessageText.Text = message;

        // Đặt chế độ xuyên chuột ngay sau khi cửa sổ được tạo
        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        // Đặt style xuyên chuột ngay khi handle cửa sổ vừa được tạo (sớm hơn cả Loaded)
        var hwnd = new WindowInteropHelper(this).Handle;
        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        // Thiết lập: cửa sổ phân lớp + xuyên chuột + tool window
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW);
    }
}
