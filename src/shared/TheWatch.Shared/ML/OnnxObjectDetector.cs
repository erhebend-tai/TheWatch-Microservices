// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace TheWatch.Shared.ML;

/// <summary>
/// ONNX Runtime-based object detector for CCTV surveillance frame analysis.
/// Loads a YOLOv8-style object detection model from the app bundle.
/// Model expects: 640x640 RGB float32 images normalized to [0,1].
/// </summary>
public sealed class OnnxObjectDetector : IObjectDetector, IDisposable
{
    private readonly ILogger<OnnxObjectDetector> _logger;
    private readonly InferenceSession? _session;
    private const int ModelInputSize = 640;
    private const float NmsIouThreshold = 0.45f;

    // COCO-style label map for object detection
    private static readonly Dictionary<int, string> LabelMap = new()
    {
        { 0, ObjectDetectionLabels.Person },
        { 1, ObjectDetectionLabels.Vehicle },
        { 2, ObjectDetectionLabels.Car },
        { 3, ObjectDetectionLabels.Truck },
        { 4, ObjectDetectionLabels.Weapon },
        { 5, ObjectDetectionLabels.Knife },
        { 6, ObjectDetectionLabels.LicensePlate },
        { 7, ObjectDetectionLabels.Backpack },
        { 8, ObjectDetectionLabels.Face },
        { 9, ObjectDetectionLabels.Fire },
        { 10, ObjectDetectionLabels.Smoke }
    };

    public bool IsReady => _session is not null;

    public OnnxObjectDetector(ILogger<OnnxObjectDetector> logger, string? modelPath = null)
    {
        _logger = logger;

        var path = modelPath ?? Path.Combine(AppContext.BaseDirectory, "Models", "yolov8_surveillance.onnx");
        if (File.Exists(path))
        {
            try
            {
                var options = new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                    InterOpNumThreads = 1,
                    IntraOpNumThreads = 4
                };
                _session = new InferenceSession(path, options);
                _logger.LogInformation("ONNX object detector loaded from {ModelPath}", path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load ONNX model from {ModelPath}, detector will operate in fallback mode", path);
            }
        }
        else
        {
            _logger.LogWarning("ONNX model not found at {ModelPath}, detector will operate in fallback mode", path);
        }
    }

    public Task<IReadOnlyList<ObjectDetectionResult>> DetectObjectsAsync(
        byte[] imageData,
        float confidenceThreshold = 0.5f,
        CancellationToken cancellationToken = default)
    {
        if (_session is null)
        {
            return Task.FromResult<IReadOnlyList<ObjectDetectionResult>>(Array.Empty<ObjectDetectionResult>());
        }

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Assume raw RGB bytes; resize/normalize to model input
            var inputTensor = PreprocessImage(imageData, ModelInputSize, ModelInputSize);
            return RunInference(inputTensor, confidenceThreshold, cancellationToken);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<ObjectDetectionResult>> DetectObjectsInFrameAsync(
        byte[] frameData,
        int width,
        int height,
        float confidenceThreshold = 0.5f,
        CancellationToken cancellationToken = default)
    {
        if (_session is null)
        {
            return Task.FromResult<IReadOnlyList<ObjectDetectionResult>>(Array.Empty<ObjectDetectionResult>());
        }

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var inputTensor = PreprocessFrame(frameData, width, height, ModelInputSize, ModelInputSize);
            return RunInference(inputTensor, confidenceThreshold, cancellationToken);
        }, cancellationToken);
    }

    private IReadOnlyList<ObjectDetectionResult> RunInference(
        DenseTensor<float> inputTensor,
        float confidenceThreshold,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("images", inputTensor)
        };

        using var results = _session!.Run(inputs);
        var output = results.First().AsTensor<float>();

        // Parse YOLO output: [1, numClasses+5, numDetections]
        var detections = ParseYoloOutput(output, confidenceThreshold);

        // Apply Non-Maximum Suppression
        var nmsResults = ApplyNms(detections, NmsIouThreshold);

        if (nmsResults.Count > 0)
        {
            _logger.LogInformation("Objects detected: {Labels}",
                string.Join(", ", nmsResults.Select(d => $"{d.Label}={d.Confidence:P1}")));
        }

        return nmsResults.AsReadOnly();
    }

    private static DenseTensor<float> PreprocessImage(byte[] imageData, int targetWidth, int targetHeight)
    {
        // Create tensor [1, 3, targetHeight, targetWidth] from raw RGB bytes
        var tensor = new DenseTensor<float>(new[] { 1, 3, targetHeight, targetWidth });
        var pixelCount = targetWidth * targetHeight;
        var srcPixelCount = imageData.Length / 3;

        for (var i = 0; i < Math.Min(pixelCount, srcPixelCount); i++)
        {
            var srcIdx = i * 3;
            var y = i / targetWidth;
            var x = i % targetWidth;

            tensor[0, 0, y, x] = imageData[srcIdx] / 255f;       // R
            tensor[0, 1, y, x] = imageData[srcIdx + 1] / 255f;   // G
            tensor[0, 2, y, x] = imageData[srcIdx + 2] / 255f;   // B
        }

        return tensor;
    }

    private static DenseTensor<float> PreprocessFrame(byte[] frameData, int srcWidth, int srcHeight, int targetWidth, int targetHeight)
    {
        var tensor = new DenseTensor<float>(new[] { 1, 3, targetHeight, targetWidth });
        var scaleX = (float)srcWidth / targetWidth;
        var scaleY = (float)srcHeight / targetHeight;

        for (var y = 0; y < targetHeight; y++)
        {
            for (var x = 0; x < targetWidth; x++)
            {
                var srcX = (int)(x * scaleX);
                var srcY = (int)(y * scaleY);
                var srcIdx = (srcY * srcWidth + srcX) * 3;

                if (srcIdx + 2 < frameData.Length)
                {
                    tensor[0, 0, y, x] = frameData[srcIdx] / 255f;
                    tensor[0, 1, y, x] = frameData[srcIdx + 1] / 255f;
                    tensor[0, 2, y, x] = frameData[srcIdx + 2] / 255f;
                }
            }
        }

        return tensor;
    }

    private static List<ObjectDetectionResult> ParseYoloOutput(Tensor<float> output, float confidenceThreshold)
    {
        var detections = new List<ObjectDetectionResult>();
        var dims = output.Dimensions;

        // YOLO output shape: [1, numAttributes, numDetections]
        // numAttributes = 4 (box) + numClasses
        if (dims.Length < 3) return detections;

        var numAttributes = dims[1];
        var numDetections = dims[2];
        var numClasses = numAttributes - 4;

        for (var d = 0; d < numDetections; d++)
        {
            // Box: cx, cy, w, h
            var cx = output[0, 0, d];
            var cy = output[0, 1, d];
            var w = output[0, 2, d];
            var h = output[0, 3, d];

            // Find best class
            var bestClassIdx = 0;
            var bestScore = 0f;
            for (var c = 0; c < numClasses && c < LabelMap.Count; c++)
            {
                var score = output[0, 4 + c, d];
                if (score > bestScore)
                {
                    bestScore = score;
                    bestClassIdx = c;
                }
            }

            if (bestScore < confidenceThreshold) continue;

            var label = LabelMap.GetValueOrDefault(bestClassIdx, "unknown");
            detections.Add(new ObjectDetectionResult
            {
                Label = label,
                DetectionType = ObjectDetectionLabels.ToDetectedObjectType(label),
                Confidence = bestScore,
                BoundingBoxX = cx - w / 2,
                BoundingBoxY = cy - h / 2,
                BoundingBoxW = w,
                BoundingBoxH = h
            });
        }

        return detections;
    }

    private static List<ObjectDetectionResult> ApplyNms(List<ObjectDetectionResult> detections, float iouThreshold)
    {
        var results = new List<ObjectDetectionResult>();
        var sorted = detections.OrderByDescending(d => d.Confidence).ToList();
        var suppressed = new bool[sorted.Count];

        for (var i = 0; i < sorted.Count; i++)
        {
            if (suppressed[i]) continue;
            results.Add(sorted[i]);

            for (var j = i + 1; j < sorted.Count; j++)
            {
                if (suppressed[j]) continue;
                if (sorted[i].Label != sorted[j].Label) continue;

                if (ComputeIou(sorted[i], sorted[j]) > iouThreshold)
                    suppressed[j] = true;
            }
        }

        return results;
    }

    private static float ComputeIou(ObjectDetectionResult a, ObjectDetectionResult b)
    {
        var x1 = Math.Max(a.BoundingBoxX, b.BoundingBoxX);
        var y1 = Math.Max(a.BoundingBoxY, b.BoundingBoxY);
        var x2 = Math.Min(a.BoundingBoxX + a.BoundingBoxW, b.BoundingBoxX + b.BoundingBoxW);
        var y2 = Math.Min(a.BoundingBoxY + a.BoundingBoxH, b.BoundingBoxY + b.BoundingBoxH);

        var intersection = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        var areaA = a.BoundingBoxW * a.BoundingBoxH;
        var areaB = b.BoundingBoxW * b.BoundingBoxH;
        var union = areaA + areaB - intersection;

        return union > 0 ? intersection / union : 0;
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
