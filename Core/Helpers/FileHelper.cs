using System.Text;

namespace Core.Helpers;

public static class FileHelper
{
    // ================================
    // TEXT FILES
    // ================================

    public static void WriteText(string path, string content, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        File.WriteAllText(path, content, encoding);
    }

    public static string ReadText(string path, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        return File.ReadAllText(path, encoding);
    }

    // Async versions
    public static async Task WriteTextAsync(string path, string content, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        await File.WriteAllTextAsync(path, content, encoding);
    }

    public static async Task<string> ReadTextAsync(string path, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        return await File.ReadAllTextAsync(path, encoding);
    }

    // ================================
    // BYTE FILES
    // ================================

    public static void WriteBytes(string path, byte[] data)
    {
        File.WriteAllBytes(path, data);
    }

    public static byte[] ReadBytes(string path)
    {
        return File.ReadAllBytes(path);
    }

    // Async versions
    public static async Task WriteBytesAsync(string path, byte[] data)
    {
        await File.WriteAllBytesAsync(path, data);
    }

    public static async Task<byte[]> ReadBytesAsync(string path)
    {
        return await File.ReadAllBytesAsync(path);
    }

    // ================================
    // STREAM (for large files)
    // ================================

    public static async Task CopyToFileAsync(Stream input, string path)
    {
        using var output = File.Create(path);
        await input.CopyToAsync(output);
    }

    public static async Task CopyFromFileAsync(string path, Stream output)
    {
        using var input = File.OpenRead(path);
        await input.CopyToAsync(output);
    }
}