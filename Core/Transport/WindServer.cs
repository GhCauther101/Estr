using Core.Config;
using Core.Helpers;
using Core.Models;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Core.Transport;

public class WindServer
{
    private readonly string title;
    private readonly Socket serverSocket;
    private readonly Channel<byte[]> channel;

    private List<EndPoint> destinations = [];
    private DataProcessor dataProcessor;

    private ChannelWriter<Noise> streamWriter;
    private ChannelReader<Noise> streamReader;
    private ChannelWriter<Notification> notificationWriter;

    public WindServer(
        ServerConfig serverConfig,
        DataProcessorConfig dataProcessorConfig,
        ChannelWriter<Noise> streamWriter,
        ChannelReader<Noise> streamReader,
        ChannelWriter<Notification> notificationWriter
    ) {
        title = serverConfig.Title;

        this.streamWriter = streamWriter;
        this.streamReader = streamReader;
        this.notificationWriter = notificationWriter;

        serverSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        serverSocket.Bind(new IPEndPoint(IPAddress.Parse(serverConfig.Address), serverConfig.Port));
        serverSocket.Listen(serverConfig.AcceptedCount);


        dataProcessor = new(dataProcessorConfig);
    }

    public IList<EndPoint> Destinations => destinations;

    public async Task Run(EndPoint endpoint, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var socket = await serverSocket.AcceptAsync();
            destinations.Add(endpoint);
            await ReadSocket(socket, endpoint);
        }
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        channel.Writer.Complete();
        cancellationToken.ThrowIfCancellationRequested();
        serverSocket.Close();
    }

    public async Task Send(byte[] data, EndPoint endpoint)
    {
        var sentData = await serverSocket.SendToAsync(data, endpoint);
    }

    private async Task ReadSocket(Socket socket, EndPoint endpoint)
    {
        try
        {
            byte[] buffer = new byte[4096];
            await socket.ReceiveFromAsync(buffer, endpoint);
            var noise = await dataProcessor.Process(buffer);
        }
        catch (Exception ex)
        {
            throw ServiceHelper.BumpError("Handled error exception.", "server", ex);
        }
    }
}
