using System.Configuration;
using System.Data;
using System.Windows;
using Velopack;

namespace PUBGMortar
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Phải chạy trước khi bất kỳ cửa sổ nào được tạo — Velopack dùng các
            // tham số dòng lệnh đặc biệt khi installer/updater gọi lại app trong
            // quá trình cài đặt/gỡ/cập nhật. Nếu app chạy trực tiếp (VD: debug),
            // hàm này trả về ngay và chương trình khởi động bình thường.
            VelopackApp.Build().Run();
        }
    }

}
