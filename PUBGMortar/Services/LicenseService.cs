using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PUBGMortar.Services;

/// <summary>
/// Xác thực key giới hạn thời gian sử dụng (số giờ tùy chỉnh lúc tạo key).
/// Key có dạng "{expiryUnixSeconds}-{signature}", ký bằng HMAC-SHA256 với
/// một secret cố định nhúng trong app — secret này phải khớp với secret trong
/// công cụ tạo key riêng (KeyGenerator/Program.cs), không nằm trong bản cài đưa
/// cho người dùng cuối.
///
/// Lưu ý: đây KHÔNG phải cơ chế bảo mật tuyệt đối - ai giải mã/decompile được
/// app vẫn lấy được secret và tự tạo key hợp lệ. Chỉ đủ để chặn người dùng phổ
/// thông không có key, không phù hợp cho mục đích bảo vệ thương mại nghiêm ngặt.
/// </summary>
public static class LicenseService
{
    // Phải khớp với Secret trong KeyGenerator/Program.cs. Đổi thành chuỗi riêng
    // của bạn trước khi build bản phát hành đầu tiên.
    private const string Secret = "KEY001122334455";

    private static string StatePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "PUBGMortar", "license.json");

    private record LicenseState(string Key, long ExpiryUnixSeconds);

    /// <summary>
    /// Kiểm tra định dạng, chữ ký, và hạn dùng của một chuỗi key.
    /// </summary>
    public static bool TryValidate(string key, out DateTime expiryUtc)
    {
        expiryUtc = default;
        var trimmed = key.Trim();
        var separatorIndex = trimmed.LastIndexOf('-');
        if (separatorIndex <= 0 || separatorIndex == trimmed.Length - 1)
            return false;

        var payload = trimmed[..separatorIndex];
        var signature = trimmed[(separatorIndex + 1)..];

        if (!long.TryParse(payload, out var expirySeconds))
            return false;

        var expectedSignature = Sign(payload);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(signature)))
        {
            return false;
        }

        expiryUtc = DateTimeOffset.FromUnixTimeSeconds(expirySeconds).UtcDateTime;
        return expiryUtc > DateTime.UtcNow;
    }

    private static string Sign(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash)[..8]; // rút gọn cho key ngắn, dễ gõ tay
    }

    /// <summary>
    /// Đọc key đã lưu từ lần kích hoạt trước (nếu có) và còn hạn thì trả về true,
    /// không bắt người dùng nhập lại key mỗi lần mở app.
    /// </summary>
    public static bool TryLoadSavedValidLicense(out DateTime expiryUtc)
    {
        expiryUtc = default;
        try
        {
            if (!File.Exists(StatePath)) return false;
            var json = File.ReadAllText(StatePath);
            var state = JsonSerializer.Deserialize<LicenseState>(json);
            if (state == null) return false;
            return TryValidate(state.Key, out expiryUtc);
        }
        catch
        {
            return false;
        }
    }

    public static void Save(string key, DateTime expiryUtc)
    {
        var dir = Path.GetDirectoryName(StatePath)!;
        Directory.CreateDirectory(dir);
        var state = new LicenseState(key, new DateTimeOffset(expiryUtc).ToUnixTimeSeconds());
        File.WriteAllText(StatePath, JsonSerializer.Serialize(state));
    }
}
