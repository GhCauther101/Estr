namespace Core.Config;

// handler space
public record HandlerConfig
(
    Guid Id,
    string Title,
    string Type,
    int Rang,
    string[] AppliedMessageTypes,
    string HandlerMarkers
);
