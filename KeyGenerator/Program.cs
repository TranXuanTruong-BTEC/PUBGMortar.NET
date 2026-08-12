using System.Security.Cryptography;
using System.Text;

// PHẢI khớp chính xác với Secret trong PUBGMortar/Services/LicenseService.cs.
// Đổi cả hai chỗ cùng lúc nếu bạn muốn đặt secret riêng.
const string Secret = "KEY001122334455";

Console.Write("Nhập số giờ hiệu lực cho key (VD: 24, 72, 720): ");
var input = Console.ReadLine();

if (!double.TryParse(input, out var hours) || hours <= 0)
{
    Console.WriteLine("Số giờ không hợp lệ.");
    return;
}

var expiry = DateTimeOffset.UtcNow.AddHours(hours);
var payload = expiry.ToUnixTimeSeconds().ToString();

using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
var signature = Convert.ToHexString(hash)[..8];

var key = $"{payload}-{signature}";

Console.WriteLine();
Console.WriteLine($"Key có hiệu lực {hours:0.##} giờ, hết hạn lúc " +
                   $"{expiry.ToLocalTime():dd/MM/yyyy HH:mm} (giờ máy bạn):");
Console.WriteLine();
Console.WriteLine(key);
Console.WriteLine();
Console.WriteLine("Gửi chuỗi key ở trên cho người dùng. KHÔNG chia sẻ công cụ này.");
