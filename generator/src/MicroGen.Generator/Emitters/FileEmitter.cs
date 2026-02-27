namespace MicroGen.Generator.Emitters;

/// <summary>
/// Writes generated content to disk, tracking all files produced.
/// </summary>
public sealed class FileEmitter
{
    private readonly List<string> _generatedFiles = [];
    private readonly bool _dryRun;

    public FileEmitter(bool dryRun = false)
    {
        _dryRun = dryRun;
    }

    public IReadOnlyList<string> GeneratedFiles => _generatedFiles;

    /// <summary>
    /// Writes content to a file, creating directories as needed.
    /// </summary>
    public async Task EmitAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.GetFullPath(filePath);
        var dir = Path.GetDirectoryName(absolutePath)!;

        if (!_dryRun)
        {
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(absolutePath, content, cancellationToken);
        }

        _generatedFiles.Add(absolutePath);
    }

    /// <summary>
    /// Emits content into a project subfolder structure.
    /// </summary>
    public async Task EmitToProjectAsync(
        string serviceRoot,
        string projectName,
        string relativePath,
        string content,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(serviceRoot, "src", projectName, relativePath);
        await EmitAsync(fullPath, content, cancellationToken);
    }
}
