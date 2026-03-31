using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace HumanRepProj.Services
{
    public class FaceRecognitionService
    {
        private readonly InferenceSession _session;

        public FaceRecognitionService(InferenceSession session)
        {
            _session = session;
        }

        public float[] GetFaceEmbedding(byte[] imageBytes)
        {
            using var ms = new MemoryStream(imageBytes);
            using var image = Image.Load<Rgb24>(ms);

            var inputTensor = PreprocessImageForFaceNet(image);
            var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", inputTensor)
        };

            using var results = _session.Run(inputs);
            var embedding = results.First().AsTensor<float>().ToArray();
            return NormalizeEmbedding(embedding);
        }

        private Tensor<float> PreprocessImageForFaceNet(Image<Rgb24> image)
        {
            const int targetSize = 112;

            var resized = image.Clone(
                Configuration.Default,
                operations => operations.Resize(new ResizeOptions
                {
                    Size = new Size(targetSize, targetSize),
                    Mode = ResizeMode.Pad,
                    Position = AnchorPositionMode.Center,
                    Sampler = KnownResamplers.Bicubic,
                    PadColor = Color.Black
                })
            );
            var tensor = new DenseTensor<float>(new[] { 1, 3, targetSize, targetSize });

            for (int y = 0; y < resized.Height; y++)
            {
                for (int x = 0; x < resized.Width; x++)
                {
                    var pixel = resized[x, y];
                    tensor[0, 0, y, x] = (pixel.R - 127.5f) / 127.5f;
                    tensor[0, 1, y, x] = (pixel.G - 127.5f) / 127.5f;
                    tensor[0, 2, y, x] = (pixel.B - 127.5f) / 127.5f;
                }
            }

            return tensor;
        }

        private static float[] NormalizeEmbedding(float[] embedding)
        {
            if (embedding == null || embedding.Length == 0)
                return Array.Empty<float>();

            double sumSquares = 0;
            for (int i = 0; i < embedding.Length; i++)
            {
                sumSquares += embedding[i] * embedding[i];
            }

            var magnitude = (float)Math.Sqrt(sumSquares);
            if (magnitude <= 1e-6f)
                return embedding;

            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= magnitude;
            }

            return embedding;
        }
    }
}