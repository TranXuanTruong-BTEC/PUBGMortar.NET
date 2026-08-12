using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace PUBGMortar.Services;

/// <summary>
/// Kiểm tra và BẮT BUỘC cài bản cập nhật mới từ GitHub Releases bằng Velopack.
/// Nếu có bản mới, app phải tải và cài xong (rồi tự khởi động lại) mới được
/// dùng tiếp — không có bản mới, hoặc không kiểm tra được do mất mạng thì vẫn
/// cho chạy bình thường (không khóa app chỉ vì offline).
/// Repo phải có các bản release được đóng gói bằng công cụ `vpk`
/// (xem README phần "Đóng gói installer & cập nhật qua GitHub").
/// </summary>
public class UpdateService
{
    // TODO: đổi thành URL repo GitHub thật của bạn trước khi build release đầu tiên
    private const string GithubRepoUrl = "https://github.com/TranXuanTruong-BTEC/PUBGMortar.NET";

    /// <summary>
    /// Trả về true nếu app được phép tiếp tục chạy (đã ở bản mới nhất, hoặc
    /// không kiểm tra được do mất mạng). Trả về false nếu có bản mới bắt buộc
    /// nhưng tải/cài đặt thất bại — trường hợp này gọi nơi khác nên đóng app.
    /// Nếu cài thành công, hàm ApplyUpdatesAndRestart sẽ tự khởi động lại tiến
    /// trình với bản mới và thoát tiến trình hiện tại, nên phần code sau đó
    /// trong thực tế hầu như không bao giờ chạy tới khi cập nhật thành công.
    /// </summary>
    public async Task<bool> CheckAndEnforceUpdateAsync(Action<string> onStatusChanged)
    {
        UpdateManager mgr;
        try
        {
            mgr = new UpdateManager(new GithubSource(GithubRepoUrl, accessToken: null, prerelease: false));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateManager init failed: {ex.Message}");
            return true; // lỗi cấu hình/khởi tạo -> không chặn người dùng vì lỗi của app
        }

        if (!mgr.IsInstalled)
        {
            // Đang chạy bản portable/dev (chưa cài qua Setup.exe) -> không ép cập nhật
            return true;
        }

        UpdateInfo? newVersion;
        try
        {
            newVersion = await mgr.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            // Không kiểm tra được (mất mạng, GitHub lỗi...) -> cho chạy tạm,
            // không khóa app chỉ vì không check được.
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
            return true;
        }

        if (newVersion == null)
        {
            return true; // đã là bản mới nhất
        }

        // Từ đây trở đi: CÓ bản mới -> bắt buộc phải cài xong mới cho chạy tiếp.
        try
        {
            onStatusChanged($"Đang tải bản cập nhật bắt buộc {newVersion.TargetFullRelease.Version}...");
            await mgr.DownloadUpdatesAsync(newVersion);

            onStatusChanged("Cập nhật xong — khởi động lại...");
            mgr.ApplyUpdatesAndRestart(newVersion); // thành công -> tự thoát tiến trình này
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Mandatory update failed: {ex.Message}");
            // Có bản mới bắt buộc nhưng tải/cài thất bại -> không cho dùng bản cũ
            return false;
        }
    }
}
