using Core.Helpers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace Core.Transport;

// server space
public record ServerConfig(string Title, string Address, int Port, int AcceptedCount);
public record ClientConfig(string Title, string Address, int Port);

public class LandServer
{
    private readonly string title;
    private readonly Socket serverSocket = null;

    private List<EndPoint> destinations = [];
    private DataProcessor dataProcessor;

    private ChannelWriter<Noise> streamWriter;
    private ChannelReader<Noise> streamReader;
    private ChannelWriter<Notification> notificationWriter;

    public LandServer(
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

        serverSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        serverSocket.Bind(new IPEndPoint(IPAddress.Parse(serverConfig.Address), serverConfig.Port));
        serverSocket.Listen(serverConfig.AcceptedCount);

        dataProcessor = new(dataProcessorConfig);
    }

    public IList<EndPoint> Destinations => destinations;

    public async Task Run(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var socket = await serverSocket.AcceptAsync();
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint!;
            destinations.Add(remoteEndPoint);
            await ReadSocket(socket);
        }
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        serverSocket.Close();
    }

    public async Task Send(byte[] data)
    {
        var sentData = await serverSocket.SendAsync(data);
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

public class WindClient
{
    private readonly string title;
    private readonly Socket clientSocket = null;
    private readonly IPEndPoint destinationEndpoint;
    private bool isConnected = false;

    private DataProcessor dataProcessor;

    private ChannelWriter<Noise> streamWriter;
    private ChannelReader<Noise> streamReader;
    private ChannelWriter<Notification> notificationWriter;

    public WindClient(
        ClientConfig serverConfig, 
        DataProcessorConfig dataProcessorConfig,
        ChannelWriter<Noise> streamWriter,
        ChannelReader<Noise> streamReader,
        ChannelWriter<Notification> notificationWriter
    ) {
        title = serverConfig.Title;

        this.streamWriter = streamWriter;
        this.streamReader = streamReader;
        this.notificationWriter = notificationWriter;

        clientSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
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
            throw ServiceHelper.BumpError("Destination endpoint not set.", "wind-client", clientSocket, data);

        if (!isConnected)
            throw ServiceHelper.BumpError("Server connection not set.", "wind-client", clientSocket, data);
        
        await clientSocket.SendAsync(data);
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!isConnected)
                throw ServiceHelper.BumpError("Server connection not set.", "wind-client", clientSocket);

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

// data processor space
public record DataProcessorConfig(
    string StartTag,
    string EndTag,
    string[] Types,
    string Delimeter
);

public class DataProcessor 
{
    private string delimeter;
    private string startTag;
    private string endTag;

    private bool isActiveTagEnabled = false;
    private List<byte[]> store = [];

    public DataProcessor(DataProcessorConfig config)
    {
        delimeter = config.Delimeter;
        startTag = config.StartTag;
        endTag = config.EndTag;
    }

    public async Task<Noise> Process(byte[] byteChunk)
    {
        if (CommonUtils.IsEmptyString(delimeter)) return null;
        
        var delimeterBytes = Encoding.UTF8.GetBytes(delimeter);
        var startTagBytes = Encoding.UTF8.GetBytes(startTag);
        var endTagBytes = Encoding.UTF8.GetBytes(endTag);

        var (frameState, splittedStream) = DataProcessorHelpers.SplitIncomeStream(byteChunk, delimeterBytes, startTagBytes, endTagBytes);
        isActiveTagEnabled = CommonUtils.IsActiveStream(frameState);

        if (!isActiveTagEnabled)
        {
            store.AddRange(splittedStream);
            return DecomposeMessageGrid();
        }
        else
        {
            store.Clear();
            return null;
        }
    }

    private Noise DecomposeMessageGrid()
    {
        string messageType = System.String.Empty;
        IDictionary<string, string> headerStruct = new Dictionary<string, string>();
        Memory<byte> dataRaws;

        if (store.Count > 0)
        {
            messageType = Encoding.UTF8.GetString(store[0]);
        }   

        if (store.Count > 1)
        {
            byte[] headerLine = store[1];
            headerStruct = DataProcessorHelpers.DecomposeHeaderset(headerLine, new byte[0]);
        }

        if (store.Count == 2)
        {
            dataRaws = store[3];
        }

        else throw ServiceHelper.BumpError("Unexpected behaviour while decoding message.", "data processor", store);

        if (CommonUtils.IsEmptyString(messageType) && CommonUtils.IsEmptySet(headerStruct) && dataRaws.IsEmpty)
        {
            var fieldstore = new Dictionary<string, object>();
            fieldstore["type"] = messageType;
            fieldstore["metadata"] = headerStruct;
            fieldstore["data"] = dataRaws.ToArray();
            throw ServiceHelper.BumpError("Could not decompose message from binary stream.", "data processor", fieldstore, store);
        }

        
        var noise = new Noise
        {
            Id = Guid.NewGuid(),
            Type = messageType,
            Metadata = headerStruct,
            Data = dataRaws.ToArray()
        };

        return noise;
    }
}

// model space
public class Noise
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public IDictionary<string, string> Metadata { get; set; }
    public byte[] Data { get; set; }
}

public record struct Notification(Guid Id, string Type, string Title);

// handler space
public record HandlerConfig(
    Guid Id,
    string Title,
    string Type,
    int Rang
);

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

public class HandlerManage
{
    public HandlerManage()
    {
        
    }

    public List<Handler> HandlerCollection { get; set; } = [];

    public async Task Start() { }

    public async Task Stop() { }

    public async Task RegisterHandler() { }

    public async Task DisableHandler() { }

    private async Task ProcessLaunch() { }
}