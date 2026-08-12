using System.Configuration;
using System.Data;
using System.Windows;
using PUBGMortar.Services;
using PUBGMortar.Views;
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

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1) Bắt buộc cập nhật: có bản mới thì phải cài xong (rồi tự khởi
            // động lại) mới cho dùng tiếp. Lỗi tải/cài -> đóng app luôn.
            var updateService = new UpdateService();
            var canContinueAfterUpdate = await updateService.CheckAndEnforceUpdateAsync(_ => { });

            if (!canContinueAfterUpdate)
            {
                MessageBox.Show(
                    "Có bản cập nhật mới nhưng không cài đặt được (có thể do mất mạng " +
                    "hoặc lỗi tải). Vui lòng kết nối mạng rồi mở lại ứng dụng.",
                    "Cần cập nhật", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            // 2) Kiểm tra key sử dụng (giới hạn theo giờ) trước khi vào app.
            if (!LicenseWindow.EnsureValidLicense())
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }

}
