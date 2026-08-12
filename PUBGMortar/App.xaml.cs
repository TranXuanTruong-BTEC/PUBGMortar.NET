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
        [STAThread]
        public static void Main(string[] args)
        {
            // Phải chạy đầu tiên, trước bất kỳ code nào khác — Velopack dùng
            // các tham số dòng lệnh đặc biệt (--veloapp-install, --veloapp-updated...)
            // khi installer/updater gọi lại app trong quá trình cài đặt/gỡ/cập nhật.
            // Nếu app không được chạy qua các bước đó (VD: chạy trực tiếp khi debug),
            // hàm này trả về ngay và chương trình chạy bình thường như dưới.
            VelopackApp.Build().Run();

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }

}
