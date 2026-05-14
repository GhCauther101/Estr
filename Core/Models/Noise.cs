namespace Core.Models;

public class Noise
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public IDictionary<string, string> Metadata { get; set; }
    public byte[] Data { get; set; }
}
