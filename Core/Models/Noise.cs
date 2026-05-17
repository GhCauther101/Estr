namespace Core.Models;

public interface IMessageNoise;

public class Noise : IMessageNoise
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public IDictionary<string, string> Metadata { get; set; }
    public byte[] Data { get; set; }
}
