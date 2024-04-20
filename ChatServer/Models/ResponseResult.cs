namespace ChatServer.Models;

public class ResponseResult
{
    public Message Message { get; set; }
    public ResponseProcessingResult ProcessingResult { get; set; }
    
    public ResponseResult(Message message, ResponseProcessingResult processingResult)
    {
        Message = message;
        ProcessingResult = processingResult;
    }
    
    public ResponseResult(Message message)
    {
        Message = message;
        ProcessingResult = ResponseProcessingResult.Ok;
    }
}