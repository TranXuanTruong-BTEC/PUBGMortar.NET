using System;
using System.Windows;
using H.Hooks;

namespace PUBGMortar.Services;

/// <summary>
/// Dịch vụ hotkey toàn cục - dùng thư viện H.Hooks để lắng nghe phím tắt toàn hệ thống
/// </summary>
public class GlobalHotkeyService : IDisposable
{
    private readonly LowLevelKeyboardHook _keyboardHook;
    private readonly LowLevelMouseHook _mouseHook;

    private bool _disposed;

    /// <summary>
    /// Sự kiện bắt đầu đo (Ctrl+Alt+Q) - quy trình đầy đủ, gồm cả đặt tỉ lệ
    /// </summary>
    public event EventHandler? StartMeasurement;

    /// <summary>
    /// Sự kiện đo nhanh (Alt+Q) - bỏ qua bước tỉ lệ, dùng tỉ lệ lần trước (lần đầu chưa có tỉ lệ sẽ tự chạy quy trình đầy đủ)
    /// Nếu cửa sổ thông báo đang mở thì đóng lại
    /// </summary>
    public event EventHandler? QuickMeasurement;

    /// <summary>
    /// Sự kiện click đặt điểm (Alt+chuột trái)
    /// </summary>
    public event EventHandler<(double X, double Y)>? PointSet;

    public GlobalHotkeyService()
    {
        _keyboardHook = new LowLevelKeyboardHook
        {
            IsExtendedMode = true,
            HandleModifierKeys = true,
        };

        _mouseHook = new LowLevelMouseHook
        {
            AddKeyboardKeys = true,  // Quan trọng: cho phép phát hiện phím bàn phím trong sự kiện chuột
            IsExtendedMode = true,
        };

        _keyboardHook.Down += OnKeyDown;
        _keyboardHook.Up += OnKeyUp;
        _mouseHook.Down += OnMouseDown;
    }

    /// <summary>
    /// Bắt đầu lắng nghe hotkey
    /// </summary>
    public void Start()
    {
        _keyboardHook.Start();
        _mouseHook.Start();
    }

    /// <summary>
    /// Dừng lắng nghe hotkey
    /// </summary>
    public void Stop()
    {
        _keyboardHook.Stop();
        _mouseHook.Stop();
    }

    private void OnKeyDown(object? sender, KeyboardEventArgs e)
    {
        if (!e.Keys.IsAlt) return;

        switch (e.CurrentKey)
        {
            case Key.Q:
                if (e.Keys.IsCtrl)
                {
                    // Ctrl+Alt+Q: quy trình đo đầy đủ (đặt lại tỉ lệ)
                    System.Diagnostics.Debug.WriteLine("Ctrl+Alt+Q detected - Starting full measurement");
                    Application.Current?.Dispatcher.Invoke(() => StartMeasurement?.Invoke(this, EventArgs.Empty));
                }
                else
                {
                    // Alt+Q: đo nhanh (bỏ qua tỉ lệ, lần đầu tự chạy quy trình đầy đủ)
                    // Nếu cửa sổ thông báo đang mở thì đóng lại, chưa có thì bắt đầu đo
                    System.Diagnostics.Debug.WriteLine("Alt+Q detected - Quick measurement or close overlay");
                    Application.Current?.Dispatcher.Invoke(() => QuickMeasurement?.Invoke(this, EventArgs.Empty));
                }
                break;
        }
    }

    private void OnKeyUp(object? sender, KeyboardEventArgs e)
    {
        // Không cần xử lý
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        // Dùng Keys.IsAlt để kiểm tra có đang giữ phím Alt không
        if (e.Keys.IsAlt && e.CurrentKey == Key.MouseLeft)
        {
            var position = ((double)e.Position.X, (double)e.Position.Y);
            System.Diagnostics.Debug.WriteLine($"Alt+Click detected at ({position.Item1}, {position.Item2})");
            Application.Current?.Dispatcher.Invoke(() => PointSet?.Invoke(this, position));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _keyboardHook.Down -= OnKeyDown;
        _keyboardHook.Up -= OnKeyUp;
        _mouseHook.Down -= OnMouseDown;

        _keyboardHook.Dispose();
        _mouseHook.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
