using Core.Helpers;
using Core.Models;
using System.Threading.Channels;

namespace Core.Handlers;

public class HandlerManager
{
    private bool _isWorkingProcess = false;
    private List<IHandler> _handlerCollection = [];

    private IDictionary<string, IHandler> _handlers = new Dictionary<string, IHandler>();

    private ChannelWriter<Noise> _streamWriter;
    private ChannelReader<Noise> _streamReader;
    
    public HandlerManager(
        ChannelWriter<Noise> streamWriter,
        ChannelReader<Noise> streamReader
    ) {
        _streamWriter = streamWriter;
        _streamReader = streamReader;
    }

    public bool IsWorkingProcess => _isWorkingProcess;

    public async Task Run()
    {
        while(_isWorkingProcess)
        {
            try
            {
                if (_streamReader.TryRead(out Noise noise))
                {
                    var noiseprint = ServiceHelper.ComposeNoisePrint(noise);
                    if (!_handlers.TryGetValue(noiseprint, out IHandler handler))
                    {
                        var handleprints = _handlers.Keys.AsEnumerable();
                        throw ServiceHelper.BumpError("Could not find a desired handler for the input noise type", "HandlerManager", noiseprint, noise, handleprints);
                    }

                    //try implement with task statement machine
                    //(for stable behaviour)
                    await handler.Handle(noise);
                }
            }
            catch (Exception exception)
            {
                _isWorkingProcess = false;
                throw ServiceHelper.BumpError(exception.Message, "HandlerManager", exception);
            }
        }
    }

    public async Task Stop() 
    {
        _isWorkingProcess = false;
    }

    public void RegisterHandler(IHandler handler)
    {
        if (!_isWorkingProcess) 
            return;

        var handlePrint = ServiceHelper.ComposeHandleprint(handler);
        _handlers[handlePrint] = handler;        
    }

    public void DisableHandler(string handlePrint) 
    {
        if (!_isWorkingProcess)
            return;

        _handlers.Remove(handlePrint);
    }

    private async ValueTask<int> ProcessLaunch() 
    {
        //-1 - error, 0 - correct
        return -1;
    }
}