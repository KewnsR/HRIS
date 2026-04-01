using QRCoder;

namespace HumanRepProj.Services
{
    public static class EmployeeQrCodeService
    {
        public static string EnsureQrCodeForEmployee(int employeeId, string webRootPath)
        {
            var qrDirectory = Path.Combine(webRootPath, "qrcodes");
            if (!Directory.Exists(qrDirectory))
            {
                Directory.CreateDirectory(qrDirectory);
            }

            var fileName = $"employee-{employeeId:D5}.png";
            var absolutePath = Path.Combine(qrDirectory, fileName);

            if (!File.Exists(absolutePath))
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(employeeId.ToString(), QRCodeGenerator.ECCLevel.Q);
                var pngQrCode = new PngByteQRCode(qrData);
                var pngBytes = pngQrCode.GetGraphic(20);
                File.WriteAllBytes(absolutePath, pngBytes);
            }

            return $"/qrcodes/{fileName}";
        }
    }
}
