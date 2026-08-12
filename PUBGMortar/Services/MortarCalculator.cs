using System;
using System.Windows;

namespace PUBGMortar.Services;

/// <summary>
/// Bộ tính toán cối - tính giá trị đặt cối từ góc nâng và khoảng cách ngang
/// </summary>
public class MortarCalculator
{
    /// <summary>
    /// Hệ số tỉ lệ (quy đổi pixel sang mét)
    /// </summary>
    public double ScaleFactor { get; private set; }

    /// <summary>
    /// Khoảng cách ngang (mét)
    /// </summary>
    public double HorizontalDistance { get; private set; }

    /// <summary>
    /// Góc nâng (độ)
    /// </summary>
    public double ElevationAngle { get; private set; }

    /// <summary>
    /// Tầm bắn tối đa của cối (mét)
    /// </summary>
    public const double MAX_DISTANCE = 700.0;

    /// <summary>
    /// FOV ngang mặc định của PUBG (độ)
    /// </summary>
    public const double DEFAULT_HORIZONTAL_FOV = 80.0;

    /// <summary>
    /// Góc nâng tối đa (độ) - tự tính theo tỉ lệ khung hình màn hình
    /// </summary>
    public double MaxDegree { get; private set; }

    /// <summary>
    /// Tọa độ Y tâm màn hình - tự tính theo độ phân giải màn hình
    /// </summary>
    public double CenterPixelY { get; private set; }

    public MortarCalculator()
    {
        UpdateScreenParameters();
    }

    /// <summary>
    /// Cập nhật tham số theo độ phân giải màn hình hiện tại
    /// </summary>
    public void UpdateScreenParameters()
    {
        // Lấy độ phân giải vật lý của màn hình chính (không bị ảnh hưởng bởi DPI scaling)
        // Dùng Win32 API để lấy độ phân giải thật
        double screenWidth = GetSystemMetrics(SM_CXSCREEN);
        double screenHeight = GetSystemMetrics(SM_CYSCREEN);

        // Tọa độ Y tâm màn hình (đếm từ 0, nên là height/2 - 0.5, làm tròn còn khoảng height/2 - 1)
        CenterPixelY = screenHeight / 2.0 - 1;

        // Tính FOV dọc theo tỉ lệ khung hình màn hình
        // PUBG dùng hệ Hor+: FOV dọc = 2 * arctan(tan(FOV ngang/2) * cao/rộng)
        double horizontalFovRad = DEFAULT_HORIZONTAL_FOV * Math.PI / 180.0;
        double verticalFovRad = 2.0 * Math.Atan(Math.Tan(horizontalFovRad / 2.0) * screenHeight / screenWidth);

        // Góc nâng tối đa bằng nửa FOV dọc
        MaxDegree = verticalFovRad * 180.0 / Math.PI / 2.0;
    }

    // Hằng số Win32 API
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    /// <summary>
    /// Đặt tỉ lệ 100 mét dựa trên 2 điểm
    /// </summary>
    /// <param name="point1">Điểm thứ nhất</param>
    /// <param name="point2">Điểm thứ hai</param>
    public void SetScaleFactor((double X, double Y) point1, (double X, double Y) point2)
    {
        double deltaX = point2.X - point1.X;
        double deltaY = point2.Y - point1.Y;
        double distanceInPixels = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

        // Khoảng cách pixel tương ứng 100 mét
        ScaleFactor = 100.0 / distanceInPixels;
    }

    /// <summary>
    /// Tính khoảng cách ngang
    /// </summary>
    /// <param name="point1">Điểm thứ nhất</param>
    /// <param name="point2">Điểm thứ hai</param>
    /// <returns>Khoảng cách ngang (mét)</returns>
    public double GetHorizontalDistance((double X, double Y) point1, (double X, double Y) point2)
    {
        double deltaX = point2.X - point1.X;
        double deltaY = point2.Y - point1.Y;
        double distanceInPixels = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

        HorizontalDistance = distanceInPixels * ScaleFactor;
        return HorizontalDistance;
    }

    /// <summary>
    /// Tính góc nâng từ một điểm trên màn hình
    /// </summary>
    /// <param name="point">Điểm trên màn hình</param>
    /// <returns>Góc nâng (độ)</returns>
    public double GetElevationAngle((double X, double Y) point)
    {
        // Tính góc đúng cách: dùng hàm arctan để quy đổi độ lệch pixel sang góc
        // deltaY > 0 nghĩa là mục tiêu ở trên tâm màn hình (góc nâng dương)
        double deltaY = CenterPixelY - point.Y;

        // Tính giá trị tan ứng với mỗi pixel: đỉnh màn hình ứng với tan(MaxDegree)
        // tanPerPixel = tan(MaxDegree) / CenterPixelY
        double maxDegreeRad = MaxDegree * Math.PI / 180.0;
        double tanPerPixel = Math.Tan(maxDegreeRad) / CenterPixelY;

        // Dùng arctan để tính góc thực tế
        ElevationAngle = Math.Atan(deltaY * tanPerPixel) * 180.0 / Math.PI;

        return ElevationAngle;
    }

    /// <summary>
    /// Tính khoảng cách cần đặt trên cối
    /// Công thức: R = (L + tan(β) * (M - √(M² - 2LM·tan(β) - L²))) / (tan²(β) + 1)
    /// </summary>
    /// <param name="beta">Góc nâng (độ)</param>
    /// <param name="L">Khoảng cách ngang (mét)</param>
    /// <returns>Khoảng cách đặt cối (mét), trả về -1 nếu vô nghiệm</returns>
    public double Solve(double beta, double L)
    {
        // Cùng mặt phẳng ngang
        if (Math.Abs(beta) < 0.001)
        {
            return L;
        }

        double tanBeta = Math.Tan(beta * Math.PI / 180.0);
        double M = MAX_DISTANCE;

        double delta = M * M - 2 * L * M * tanBeta - L * L;

        if (delta < 0)
        {
            // Vô nghiệm
            return -1;
        }

        double intermediate = M - Math.Sqrt(delta);
        double result = (L + tanBeta * intermediate) / (tanBeta * tanBeta + 1);

        return result;
    }

    /// <summary>
    /// Tính kết quả từ góc nâng và khoảng cách ngang đang lưu
    /// </summary>
    /// <returns>Khoảng cách đặt cối (mét)</returns>
    public double Solve()
    {
        return Solve(ElevationAngle, HorizontalDistance);
    }

    /// <summary>
    /// Đặt lại trạng thái bộ tính toán
    /// </summary>
    public void Reset()
    {
        ScaleFactor = 0;
        HorizontalDistance = 0;
        ElevationAngle = 0;
    }
}
