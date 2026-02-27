using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace TheWatch.Shared.ML;

/// <summary>
/// ONNX Runtime-based audio classifier for gunshot, explosion, and scream detection.
/// Loads a YAMNet-style audio classification model from the app bundle.
/// Model expects: 16kHz mono PCM float32 frames.
/// </summary>
public sealed class OnnxAudioClassifier : IAudioClassifier, IDisposable
{
    private readonly ILogger<OnnxAudioClassifier> _logger;
    private readonly InferenceSession? _session;
    private readonly string[] _labels;

    // Standard YAMNet/AudioSet labels relevant to emergency detection
    private static readonly Dictionary<int, string> EmergencyLabelMap = new()
    {
        { 0, AudioLabels.Background },
        { 1, AudioLabels.Gunshot },
        { 2, AudioLabels.Explosion },
        { 3, AudioLabels.Scream },
        { 4, AudioLabels.GlassBreak },
        { 5, AudioLabels.Siren }
    };

    public bool IsReady => _session is not null;

    public OnnxAudioClassifier(ILogger<OnnxAudioClassifier> logger, string? modelPath = null)
    {
        _logger = logger;
        _labels = EmergencyLabelMap.Values.ToArray();

        var path = modelPath ?? Path.Combine(AppContext.BaseDirectory, "Models", "gunshot_detector.onnx");
        if (File.Exists(path))
        {
            try
            {
                var options = new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                    InterOpNumThreads = 1,
                    IntraOpNumThreads = 2
                };
                _session = new InferenceSession(path, options);
                _logger.LogInformation("ONNX audio classifier loaded from {ModelPath}", path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load ONNX model from {ModelPath}, classifier will operate in fallback mode", path);
            }
        }
        else
        {
            _logger.LogWarning("ONNX model not found at {ModelPath}, classifier will operate in fallback mode", path);
        }
    }

    public Task<IReadOnlyList<AudioClassificationResult>> ClassifyAsync(
        byte[] audioData,
        int sampleRate = 16000,
        float confidenceThreshold = 0.7f,
        CancellationToken cancellationToken = default)
    {
        if (_session is null)
        {
            // Fallback: return empty results when model is not available
            return Task.FromResult<IReadOnlyList<AudioClassificationResult>>(Array.Empty<AudioClassificationResult>());
        }

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Convert PCM bytes to float32 samples
            var floatSamples = ConvertToFloat32(audioData);
            var audioDuration = TimeSpan.FromSeconds((double)floatSamples.Length / sampleRate);

            // Create input tensor [1, num_samples]
            var tensor = new DenseTensor<float>(floatSamples, new[] { 1, floatSamples.Length });
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("audio_input", tensor)
            };

            // Run inference
            using var results = _session.Run(inputs);
            var output = results.First().AsEnumerable<float>().ToArray();

            // Apply softmax to get probabilities
            var probabilities = Softmax(output);

            // Filter by confidence threshold
            var classifications = new List<AudioClassificationResult>();
            for (var i = 0; i < Math.Min(probabilities.Length, EmergencyLabelMap.Count); i++)
            {
                if (probabilities[i] >= confidenceThreshold && EmergencyLabelMap.TryGetValue(i, out var label))
                {
                    classifications.Add(new AudioClassificationResult
                    {
                        Label = label,
                        Confidence = probabilities[i],
                        AudioDuration = audioDuration,
                        ModelVersion = "1.0.0"
                    });
                }
            }

            if (classifications.Count > 0)
            {
                _logger.LogInformation("Audio classified: {Labels}",
                    string.Join(", ", classifications.Select(c => $"{c.Label}={c.Confidence:P1}")));
            }

            return (IReadOnlyList<AudioClassificationResult>)classifications.AsReadOnly();
        }, cancellationToken);
    }

    private static float[] ConvertToFloat32(byte[] pcmBytes)
    {
        // Assume 16-bit PCM little-endian
        var sampleCount = pcmBytes.Length / 2;
        var samples = new float[sampleCount];
        for (var i = 0; i < sampleCount; i++)
        {
            var sample = BitConverter.ToInt16(pcmBytes, i * 2);
            samples[i] = sample / 32768f; // Normalize to [-1, 1]
        }
        return samples;
    }

    private static float[] Softmax(float[] logits)
    {
        var maxLogit = logits.Max();
        var exps = logits.Select(l => MathF.Exp(l - maxLogit)).ToArray();
        var sumExps = exps.Sum();
        return exps.Select(e => e / sumExps).ToArray();
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
