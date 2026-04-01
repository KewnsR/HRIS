using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using HumanRepProj.Data;
using HumanRepProj.Models;
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML.OnnxRuntime.Tensors;
using static System.Net.Mime.MediaTypeNames;
using HumanRepProj.Services;

namespace HumanRepProj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOnnxFaceDetectionService _faceDetectionService;
        private readonly FaceRecognitionService _faceRecognitionService; // Add this

        public AttendanceController(
            ApplicationDbContext context,
            IOnnxFaceDetectionService faceDetectionService,
            FaceRecognitionService faceRecognitionService) // Inject service
        {
            _context = context;
            _faceDetectionService = faceDetectionService;
            _faceRecognitionService = faceRecognitionService;
        }

        [HttpPost("log")]
        public async Task<IActionResult> LogAttendance([FromBody] AttendanceDto dto)
        {
            var sessionEmployeeId = HttpContext.Session.GetInt32("EmployeeID");
            if (!sessionEmployeeId.HasValue || sessionEmployeeId.Value <= 0)
            {
                return Unauthorized(new { success = false, message = "Employee session is required." });
            }

            if (dto.EmployeeId != sessionEmployeeId.Value)
            {
                return Forbid();
            }

            var employee = await _context.Employees.FindAsync(dto.EmployeeId);
            if (employee == null)
                return NotFound("Employee not found");

            var today = DateTime.UtcNow.Date;
            var now = DateTime.UtcNow;

            var attendance = await _context.AttendanceRecords
                .FirstOrDefaultAsync(a => a.EmployeeID == dto.EmployeeId && a.AttendanceDate == today);

            if (attendance == null)
            {
                attendance = new AttendanceRecord
                {
                    EmployeeID = dto.EmployeeId,
                    AttendanceDate = today,
                    Status = "Present",
                    TimeIn = now.TimeOfDay,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.AttendanceRecords.Add(attendance);
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    success = true,
                    action = "check-in",
                    message = "Checked in successfully.",
                    time = attendance.TimeIn?.ToString(@"hh\:mm\:ss")
                });
            }

            if (!attendance.TimeOut.HasValue)
            {
                attendance.TimeOut = now.TimeOfDay;
                attendance.UpdatedAt = now;
                _context.AttendanceRecords.Update(attendance);
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    success = true,
                    action = "check-out",
                    message = "Checked out successfully.",
                    time = attendance.TimeOut?.ToString(@"hh\:mm\:ss")
                });
            }

            return BadRequest(new
            {
                success = false,
                action = "completed",
                message = "Attendance already completed for today."
            });
        }

        [HttpPost("verify-face")]
        public async Task<IActionResult> VerifyFace([FromBody] FaceVerifyDto dto)
        {
            var sessionEmployeeId = HttpContext.Session.GetInt32("EmployeeID");
            if (!sessionEmployeeId.HasValue || sessionEmployeeId.Value <= 0)
            {
                return Unauthorized("Employee session is required");
            }

            if (dto.EmployeeId != sessionEmployeeId.Value)
            {
                return Forbid();
            }

            // 1. Face Detection
            var detectionResult = await DetectFace(dto.Image);
            var detectionValue = detectionResult.Value;
            if (detectionValue == null || !detectionValue.Faces.Any())
                return BadRequest("No faces detected");


            // 2. Get Employee Face Data
            var employee = await _context.Employees
                .Include(e => e.FaceData)
                .FirstOrDefaultAsync(e => e.EmployeeID == dto.EmployeeId);

            if (employee?.FaceData == null)
                return NotFound("Employee face data not found");

            // 3. Face Comparison (Simplified - implement actual comparison)
            var faceData = employee.FaceData as FaceData;
            var match = CompareFaces(faceData?.OriginalImage, dto.Image);

            return Ok(new FaceVerifyResult
            {
                Match = match,
                Confidence = match ? 0.95f : 0.1f // Example values
            });
        }

        [HttpPost("detect-face")]
        public async Task<ActionResult<FaceDetectionResult>> DetectFace([FromBody] string imageData)
        {
            try
            {
                // Convert base64 to image
                var imageBytes = Convert.FromBase64String(imageData.Split(',').Last());
                using var ms = new MemoryStream(imageBytes);
                using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(ms);

                // Preprocess image for YOLO
                var inputTensor = PreprocessImage(image);

                // Create input for ONNX
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("images", inputTensor)
                };

                // Run inference
                var results = _faceDetectionService.Session.Run(inputs);
                var output = results.First().AsTensor<float>(); // Add this
                var faces = ParseYoloOutput(output); // Use parsed output

                return new FaceDetectionResult
                {
                    Success = true,
                    Faces = faces,
                    Message = $"Detected {faces.Count} faces"
                };
            }
            catch (Exception ex)
            {
                return BadRequest(new FaceDetectionResult
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        private bool CompareFaces(string referenceImageBase64, string testImageBase64)
        {
            var referenceEmbedding = GetFaceEmbedding(referenceImageBase64, cropLargestFace: true);
            var testEmbedding = GetFaceEmbedding(testImageBase64, cropLargestFace: true);

            float similarity = CosineSimilarity(referenceEmbedding, testEmbedding);
            return similarity >= 0.75f;
        }

        private float[]? GetFaceEmbedding(string base64Image, bool cropLargestFace)
        {
            try
            {
                var imageBytes = Convert.FromBase64String(base64Image.Split(',').Last());

                if (cropLargestFace)
                {
                    var croppedFaceBytes = TryCropLargestDetectedFace(imageBytes);
                    if (croppedFaceBytes != null)
                    {
                        imageBytes = croppedFaceBytes;
                    }
                }

                return _faceRecognitionService.GetFaceEmbedding(imageBytes); // Use injected service
            }
            catch
            {
                return null;
            }
        }

        private byte[]? TryCropLargestDetectedFace(byte[] imageBytes)
        {
            try
            {
                using var ms = new MemoryStream(imageBytes);
                using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(ms);

                var inputTensor = PreprocessImage(image);
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("images", inputTensor)
                };

                using var results = _faceDetectionService.Session.Run(inputs);
                var output = results.First().AsTensor<float>();
                var faces = ParseYoloOutput(output);

                if (faces.Count == 0)
                    return null;

                var largestFace = faces
                    .OrderByDescending(f => f.Width * f.Height)
                    .First();

                var marginX = largestFace.Width * 0.15f;
                var marginY = largestFace.Height * 0.15f;

                var x = (int)MathF.Max(0, largestFace.X - marginX);
                var y = (int)MathF.Max(0, largestFace.Y - marginY);
                var right = (int)MathF.Min(image.Width, largestFace.X + largestFace.Width + marginX);
                var bottom = (int)MathF.Min(image.Height, largestFace.Y + largestFace.Height + marginY);

                var width = right - x;
                var height = bottom - y;

                if (width <= 0 || height <= 0)
                    return null;

                using var faceCrop = image.Clone(ctx => ctx.Crop(new Rectangle(x, y, width, height)));
                using var outStream = new MemoryStream();
                faceCrop.Save(outStream, new JpegEncoder());
                return outStream.ToArray();
            }
            catch
            {
                return null;
            }
        }

        private float CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA == null || vectorB == null || vectorA.Length != vectorB.Length)
                return 0f;

            float dot = 0f, magA = 0f, magB = 0f;
            for (int i = 0; i < vectorA.Length; i++)
            {
                dot += vectorA[i] * vectorB[i];
                magA += vectorA[i] * vectorA[i];
                magB += vectorB[i] * vectorB[i];
            }

            var denominator = (float)(Math.Sqrt(magA) * Math.Sqrt(magB));
            if (denominator <= 1e-6f)
                return 0f;

            return dot / denominator;
        }


        private Tensor<float> PreprocessImage(Image<Rgb24> image)
        {
            const int targetSize = 640;
            // Resize
            using var resized = image.Clone(x => x.Resize(targetSize, targetSize));
            // Normalize and convert to tensor [1, 3, 640, 640]
            var tensor = new DenseTensor<float>(new[] { 1, 3, targetSize, targetSize });
            for (int y = 0; y < resized.Height; y++)
            {
                for (int x = 0; x < resized.Width; x++)
                {
                    var pixel = resized[x, y];
                    tensor[0, 0, y, x] = pixel.R / 255f; // Red
                    tensor[0, 1, y, x] = pixel.G / 255f; // Green
                    tensor[0, 2, y, x] = pixel.B / 255f; // Blue
                }
            }
            return tensor;
        }

        private List<FaceBox> ParseYoloOutput(Tensor<float> output)
        {
            const float confidenceThreshold = 0.5f;
            var detectedFaces = new List<FaceBox>();

            // YOLOv8 output shape: [batch, num_anchors, 16] where 16 = [cx, cy, w, h, angle, conf, 10 landmarks]
            for (int i = 0; i < output.Dimensions[1]; i++) // Iterate over anchors
            {
                float confidence = output[0, i, 4]; // Confidence score
                if (confidence >= confidenceThreshold)
                {
                    float cx = output[0, i, 0];
                    float cy = output[0, i, 1];
                    float width = output[0, i, 2];
                    float height = output[0, i, 3];

                    // Convert to top-left + width/height
                    float x = cx - width / 2;
                    float y = cy - height / 2;

                    detectedFaces.Add(new FaceBox
                    {
                        X = x,
                        Y = y,
                        Width = width,
                        Height = height,
                        Confidence = confidence
                    });
                }
            }
            return detectedFaces;
        }
    }

    public class AttendanceDto
    {
        public int EmployeeId { get; set; }
    }

    public class FaceVerifyDto
    {
        public int EmployeeId { get; set; }
        public string Image { get; set; }
    }

    public class FaceVerifyResult
    {
        public bool Match { get; set; }
        public float Confidence { get; set; }
    }

    public class FaceDetectionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<FaceBox> Faces { get; set; }
    }

    public class FaceBox
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Confidence { get; set; }
    }
    public class FaceData
    {
        public int FaceDataID { get; set; }
        public int EmployeeID { get; set; }
        public string OriginalImage { get; set; } // base64 string or URL
    }

}