using Core.Config;
using Core.Models;
using System.Threading.Channels;

namespace Core.Handlers;

public class Handler
{
    private ChannelWriter<Noise> streamWriter;
    private ChannelReader<Noise> streamReader;
    private ChannelWriter<Notification> notificationWriter;

    public Handler(
        HandlerConfig handlerConfig, 
        ChannelWriter<Noise> streamWriter, 
        ChannelReader<Noise> streamReader, 
        ChannelWriter<Notification> notificationWriter
    ){
        Id = handlerConfig.Id;
        Type = handlerConfig.Type;
        Rang = handlerConfig.Rang;
        Title = handlerConfig.Title;

        this.streamWriter = streamWriter;
        this.streamReader = streamReader;
        this.notificationWriter = notificationWriter;
    }

    public Guid Id { get; set; }
    
    public string Type { get; set; }

    public int Rang { get; set; }

    public string Title { get; set; }

    public async Task Handle(CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested) 
        {
            
        }
    }

    public async Task Disable()
    {

    }

    public async Task Process()
    {

    }

    public async Task Pipe()
    {

    }
}
