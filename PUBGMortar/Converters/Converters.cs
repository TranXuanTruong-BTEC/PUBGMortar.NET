using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PUBGMortar.Converters;

/// <summary>
/// Quy đổi giá trị bool sang Brush (dùng để hiển thị trạng thái)
/// </summary>
public class BoolToAppearanceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isListening)
        {
            return isListening ? "Success" : "Caution";
        }
        return "Secondary";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Quy đổi giá trị bool sang màu Brush
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(0x10, 0x7C, 0x10));
    private static readonly SolidColorBrush CautionBrush = new(Color.FromRgb(0xD4, 0xA0, 0x00));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isListening)
        {
            return isListening ? SuccessBrush : CautionBrush;
        }
        return CautionBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Quy đổi giá trị bool sang nội dung nút theo dõi
/// </summary>
public class BoolToListenTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isListening)
        {
            return isListening ? "Tạm dừng" : "Bắt đầu";
        }
        return "Theo dõi";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Quy đổi giá trị bool sang icon nút theo dõi (ở đây trả về ký tự văn bản)
/// </summary>
public class BoolToListenIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isListening)
        {
            return isListening ? "⏸" : "▶";
        }
        return "▶";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
