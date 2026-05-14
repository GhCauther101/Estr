namespace Core.Handlers;

public class HandlerManager
{
    public HandlerManager()
    {
        
    }

    public List<Handler> HandlerCollection { get; set; } = [];

    public async Task Start() { }

    public async Task Stop() { }

    public async Task RegisterHandler() { }

    public async Task DisableHandler() { }

    private async Task ProcessLaunch() { }
}