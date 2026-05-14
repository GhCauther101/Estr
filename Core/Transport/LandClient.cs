using Core.Config;
using Core.Helpers;
using Core.Models;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Core.Transport;

public class LandClient
{
    private readonly string title;
    private readonly Socket clientSocket = null;
    private readonly IPEndPoint destinationEndpoint;
    private bool isConnected = false;

    private DataProcessor dataProcessor;

    private ChannelWriter<Noise> streamWriter;
    private ChannelReader<Noise> streamReader;
    private ChannelWriter<Notification> notificationWriter;

    public LandClient(
        ClientConfig serverConfig, 
        DataProcessorConfig dataProcessorConfig,
        ChannelWriter<Noise> streamWriter,
        ChannelReader<Noise> streamReader,
        ChannelWriter<Notification> notificationWriter
    ){
        title = serverConfig.Title;
        
        this.streamWriter = streamWriter;
        this.streamReader = streamReader;
        this.notificationWriter = notificationWriter;

        clientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        destinationEndpoint = new IPEndPoint(IPAddress.Parse(serverConfig.Address), serverConfig.Port);

        dataProcessor = new(dataProcessorConfig);
    }

    public bool IsConnected => isConnected;

    public bool IsStatusConnected => clientSocket.Connected;

    public async Task Connect()
    {
        await clientSocket.ConnectAsync(destinationEndpoint);
        isConnected = true;
    }

    public async Task Send(byte[] data)
    {
        if (destinationEndpoint == null)
            throw ServiceHelper.BumpError("Destination endpoint not set.", "land-client", clientSocket, data);

        if (!isConnected)
            throw ServiceHelper.BumpError("Server connection not set.", "land-client", clientSocket, data);

        await clientSocket.SendAsync(data);
    }

    public async Task Run(CancellationToken cancellationToken)
    {        
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!isConnected)
                throw ServiceHelper.BumpError("Server connection not set.", "land-client", clientSocket);

            await ReadSocket(clientSocket);
        }
    }

    public void Stop()
    {
        if (clientSocket == null || !IsStatusConnected)
            return;

        try
        {
            if (IsConnected && clientSocket != null)
                clientSocket.Shutdown(SocketShutdown.Both);
        }
        catch (Exception ex)
        {
            throw ServiceHelper.BumpError("handled exception while disconnecting.", "client", ex);
        }

        isConnected = false;
        clientSocket?.Close();
        clientSocket?.Dispose();
    }
    
    private async Task ReadSocket(Socket socket)
    {
        try
        {
            byte[] buffer = new byte[4096];

            int received = await socket.ReceiveAsync(buffer, SocketFlags.None);
            var noise = await dataProcessor.Process(buffer);
            //push to handler pipeline
        }
        catch (Exception ex)
        {
            throw ServiceHelper.BumpError("Handled error exception.", "server", ex);
        }
    }
}
