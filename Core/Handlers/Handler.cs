using Core.Config;
using Core.Helpers;
using Core.Models;
using System.Diagnostics.Contracts;
using System.Threading.Channels;

namespace Core.Handlers;

public interface IHandler
{
    public Guid Id { get; set; }

    public string Type { get; set; }

    public string Title { get; set; }
    
    public int Rang { get; set; }

    public string[] ApplyiedMessageTypes { get; }

    public string HandlerMarker { get; }

    public Task Handle(IMessageNoise noise);
}

public class Handler : IHandler
{
    private ChannelWriter<Noise> streamWriter;
    private ChannelReader<Noise> streamReader;
    private ChannelWriter<Notification> notificationWriter;
    private ChannelReader<Notification> notificationReader;

    private readonly string[] _appliedTypes = [];
    private readonly string _handlerMarker;

    public Handler(HandlerConfig handlerConfig){
        Id = handlerConfig.Id;
        Type = handlerConfig.Type;
        Rang = handlerConfig.Rang;
        Title = handlerConfig.Title;

        _appliedTypes = handlerConfig.AppliedMessageTypes;
        _handlerMarker = handlerConfig.HandlerMarkers;
    }

    public Guid Id { get; set; }
    
    public string Type { get; set; }

    public int Rang { get; set; }

    public string Title { get; set; }

    public ChannelWriter<Noise> StreamWriter { get => streamWriter; init => streamWriter = value; }

    public ChannelReader<Noise> StreamReader { get => streamReader; init => streamReader = value; }

    public ChannelWriter<Notification> NotificationWriter { get => notificationWriter; init => notificationWriter = value; }

    public ChannelReader<Notification> NotificationReader { get => notificationReader; init => notificationReader = value; }

    public bool IsStreamable => streamWriter != null && streamReader != null && notificationWriter != null;

    public string[] ApplyiedMessageTypes => _appliedTypes;
    public string HandlerMarker => _handlerMarker;

    public async Task Handle(IMessageNoise noise)
    {
        if (!IsStreamable) return;

    }

    private async Task Process()
    {

    }

}
