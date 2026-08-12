using System.Windows;
using System.Windows.Input;
using PUBGMortar.Services;

namespace PUBGMortar.Views;

public partial class LicenseWindow : Window
{
    public bool Activated { get; private set; }

    public LicenseWindow()
    {
        InitializeComponent();
    }

    private void ActivateButton_Click(object sender, RoutedEventArgs e)
    {
        TryActivate();
    }

    private void KeyInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TryActivate();
        }
    }

    private void TryActivate()
    {
        if (LicenseService.TryValidate(KeyInput.Text, out var expiryUtc))
        {
            LicenseService.Save(KeyInput.Text.Trim(), expiryUtc);
            Activated = true;
            DialogResult = true;
            Close();
        }
        else
        {
            ErrorText.Text = "Key không hợp lệ hoặc đã hết hạn.";
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Activated = false;
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Đảm bảo có license hợp lệ trước khi cho dùng app: dùng lại key đã lưu nếu
    /// còn hạn, nếu không thì hiện cửa sổ nhập key. Trả về false nếu người dùng
    /// không có key hợp lệ (app nên đóng lại).
    /// </summary>
    public static bool EnsureValidLicense()
    {
        if (LicenseService.TryLoadSavedValidLicense(out _))
        {
            return true;
        }

        var window = new LicenseWindow();
        var result = window.ShowDialog();
        return result == true && window.Activated;
    }
}
