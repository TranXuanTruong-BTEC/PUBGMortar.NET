using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PUBGMortar.Services;
using PUBGMortar.Views;

namespace PUBGMortar.ViewModels;

/// <summary>
/// ViewModel của cửa sổ chính
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly MortarCalculator _calculator;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly UpdateService _updateService;

    private MeasurementStep _currentStep = MeasurementStep.Idle;
    private (double X, double Y)? _tempPoint;
    private OverlayWindow? _overlayWindow;
    private bool _hasValidScale;  // Đã có tỉ lệ hợp lệ hay chưa

    [ObservableProperty]
    private string _statusText = "Sẵn sàng";

    [ObservableProperty]
    private string _resultText = "--";

    [ObservableProperty]
    private string _horizontalDistanceText = "--";

    [ObservableProperty]
    private string _elevationAngleText = "--";

    [ObservableProperty]
    private bool _isListening = true;

    public MainViewModel()
    {
        _calculator = new MortarCalculator();
        _hotkeyService = new GlobalHotkeyService();
        _updateService = new UpdateService();

        _hotkeyService.StartMeasurement += OnStartMeasurement;
        _hotkeyService.QuickMeasurement += OnQuickMeasurement;
        _hotkeyService.PointSet += OnPointSet;

        _hotkeyService.Start();

        _ = _updateService.CheckAndApplyUpdateAsync(status => StatusText = status);
    }

    [RelayCommand]
    private void ToggleListening()
    {
        IsListening = !IsListening;
        if (IsListening)
        {
            _hotkeyService.Start();
            StatusText = "Sẵn sàng";
        }
        else
        {
            _hotkeyService.Stop();
            StatusText = "Đã tạm dừng";
        }
    }

    [RelayCommand]
    private void ResetMeasurement()
    {
        _calculator.Reset();
        _currentStep = MeasurementStep.Idle;
        _tempPoint = null;

        ResultText = "--";
        HorizontalDistanceText = "--";
        ElevationAngleText = "--";
        StatusText = "Sẵn sàng";

        CloseOverlay();
    }

    private void OnStartMeasurement(object? sender, EventArgs e)
    {
        if (!IsListening) return;

        _calculator.Reset();
        _currentStep = MeasurementStep.ScalePoint1;
        _tempPoint = null;

        ShowOverlay("Đặt tỉ lệ 100 mét: điểm thứ nhất");
    }

    /// <summary>
    /// Đo nhanh - bỏ qua bước đặt tỉ lệ, dùng tỉ lệ của lần trước
    /// Nếu cửa sổ thông báo đang mở thì đóng lại, chưa có thì bắt đầu đo
    /// </summary>
    private void OnQuickMeasurement(object? sender, EventArgs e)
    {
        if (!IsListening) return;

        // Nếu cửa sổ thông báo đang mở thì đóng lại và hủy phép đo hiện tại
        if (_overlayWindow != null)
        {
            CloseOverlay();
            _currentStep = MeasurementStep.Idle;
            _tempPoint = null;
            StatusText = "Đã hủy";
            return;
        }

        if (!_hasValidScale)
        {
            // Nếu chưa có tỉ lệ hợp lệ, quay lại quy trình đầy đủ
            OnStartMeasurement(sender, e);
            return;
        }

        _currentStep = MeasurementStep.DistancePoint1;
        _tempPoint = null;

        ShowOverlay("Đo nhanh: điểm thứ nhất (vị trí của bạn)");
    }

    private void OnPointSet(object? sender, (double X, double Y) point)
    {
        if (!IsListening) return;

        switch (_currentStep)
        {
            case MeasurementStep.ScalePoint1:
                _tempPoint = point;
                _currentStep = MeasurementStep.ScalePoint2;
                ShowOverlay("Đặt tỉ lệ 100 mét: điểm thứ hai");
                break;

            case MeasurementStep.ScalePoint2:
                if (_tempPoint.HasValue)
                {
                    _calculator.SetScaleFactor(_tempPoint.Value, point);
                    _hasValidScale = true;  // Đánh dấu đã có tỉ lệ hợp lệ
                }
                _currentStep = MeasurementStep.DistancePoint1;
                _tempPoint = null;
                ShowOverlay("Đo khoảng cách: điểm thứ nhất (vị trí của bạn)");
                break;

            case MeasurementStep.DistancePoint1:
                _tempPoint = point;
                _currentStep = MeasurementStep.DistancePoint2;
                ShowOverlay("Đo khoảng cách: điểm thứ hai (vị trí mục tiêu)");
                break;

            case MeasurementStep.DistancePoint2:
                if (_tempPoint.HasValue)
                {
                    var distance = _calculator.GetHorizontalDistance(_tempPoint.Value, point);
                    HorizontalDistanceText = $"{distance:F1} m";
                }
                _currentStep = MeasurementStep.ElevationPoint;
                _tempPoint = null;
                ShowOverlay($"Khoảng cách ngang: {_calculator.HorizontalDistance:F1}m\nĐặt góc nâng: ngắm mục tiêu rồi click");
                break;

            case MeasurementStep.ElevationPoint:
                var angle = _calculator.GetElevationAngle(point);
                ElevationAngleText = $"{angle:F2}°";

                var result = _calculator.Solve();
                if (result < 0)
                {
                    ResultText = "Vô nghiệm";
                    ShowOverlay($"Khoảng cách: {_calculator.HorizontalDistance:F0}m | Góc nâng: {angle:F1}°\n⚠ Mục tiêu ngoài tầm bắn");  // Hiển thị liên tục, không tự đóng
                }
                else
                {
                    ResultText = $"{result:F0} m";
                    // Hiển thị đầy đủ: khoảng cách gốc, góc nâng, khoảng cách đặt cối
                    ShowOverlay($"Khoảng cách: {_calculator.HorizontalDistance:F0}m | Góc nâng: {angle:F1}°\n🎯 Đặt cối: {result:F0} m");  // Hiển thị liên tục, không tự đóng
                }

                _currentStep = MeasurementStep.Idle;
                StatusText = "Đo xong (Alt+Q để đóng thông báo)";
                break;
        }
    }

    private void ShowOverlay(string message, int? autoCloseMs = null) // hiển thị thông báo nổi
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CloseOverlay();

            _overlayWindow = new OverlayWindow(message);
            _overlayWindow.Show();

            if (autoCloseMs.HasValue)
            {
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(autoCloseMs.Value)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    CloseOverlay();
                };
                timer.Start();
            }
        });

        StatusText = message.Split('\n')[0];
    }

    private void CloseOverlay()
    {
        _overlayWindow?.Close();
        _overlayWindow = null;
    }

    public void Dispose()
    {
        _hotkeyService.StartMeasurement -= OnStartMeasurement;
        _hotkeyService.QuickMeasurement -= OnQuickMeasurement;
        _hotkeyService.PointSet -= OnPointSet;
        _hotkeyService.Dispose();
        CloseOverlay();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Enum các bước đo
/// </summary>
public enum MeasurementStep
{
    Idle,
    ScalePoint1,
    ScalePoint2,
    DistancePoint1,
    DistancePoint2,
    ElevationPoint
}
