namespace Core.Config;

// data processor space
public record DataProcessorConfig
(
    string StartTag,
    string EndTag,
    string[] Types,
    string Delimeter
);
