using Core.Config;
using Core.Helpers;
using Core.Models;
using System.Text;

namespace Core.DataProcessor;

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
