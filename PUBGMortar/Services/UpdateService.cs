using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace PUBGMortar.Services;

/// <summary>
/// Tự động kiểm tra và cài bản cập nhật mới từ GitHub Releases bằng Velopack.
/// Repo phải có các bản release được đóng gói bằng công cụ `vpk`
/// (xem README phần "Đóng gói installer & cập nhật qua GitHub").
/// </summary>
public class UpdateService
{
    // TODO: đổi thành URL repo GitHub thật của bạn trước khi build release đầu tiên
    private const string GithubRepoUrl = "https://github.com/<tai-khoan-cua-ban>/PUBGMortar.NET";

    public async Task CheckAndApplyUpdateAsync(Action<string> onStatusChanged)
    {
        try
        {
            var mgr = new UpdateManager(new GithubSource(GithubRepoUrl, accessToken: null, prerelease: false));

            if (!mgr.IsInstalled)
            {
                // Đang chạy bản portable/dev (chưa cài qua Setup.exe) -> bỏ qua, không có gì để cập nhật
                return;
            }

            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                return; // đã là bản mới nhất
            }

            onStatusChanged($"Đang tải bản cập nhật {newVersion.TargetFullRelease.Version}...");
            await mgr.DownloadUpdatesAsync(newVersion);

            onStatusChanged("Cập nhật xong — khởi động lại sau 3 giây...");
            await Task.Delay(3000);
            mgr.ApplyUpdatesAndRestart(newVersion);
        }
        catch (Exception ex)
        {
            // Không có mạng, repo chưa có release nào, v.v. -> im lặng bỏ qua, không làm phiền người dùng
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }
}
