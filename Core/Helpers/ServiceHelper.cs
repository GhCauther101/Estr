using Core.Handlers;
using Core.Metaprints;
using Core.Models;

namespace Core.Helpers;

public static class ServiceHelper
{
    public static Exception BumpError(string message, string environmentLocation = "", object detail1 = null, object detail2 = null, object detail3 = null)
    {
        var banner = ComposeLocationBanner(environmentLocation);
        var line = $"{banner} {message}";

        var exception = new Exception(line);
        exception.Data[ErrorMetaprints.Detail1] = detail1;
        exception.Data[ErrorMetaprints.Detail2] = detail2;
        exception.Data[ErrorMetaprints.Detail3] = detail3;
        return exception;
    }

    public static string ComposeLocationBanner(string environmentLocation)
    {
        return $"{environmentLocation} >";
    }

    public static string ComposeHandleprint(IHandler handler) 
    {
        return $"{String.Join("!", handler.ApplyiedMessageTypes)}#{handler.HandlerMarker}";
    }

    public static string ComposeNoisePrint(Noise noise)
    {
        var messageType = noise.Metadata[NoiseMetaprints.MESSAGE_TYPES];
        var marker = noise.Metadata[NoiseMetaprints.MARKER];

        return $"{String.Join("!", messageType)}#{marker}";
    }
}