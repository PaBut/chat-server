using System.Text;
using ChatServer.Enums;
using ChatServer.SocketClients.Utilities.Tcp;

namespace ChatServer.UnitTests;

public class TcpMessageQueueTests
{
    private TcpMessageCoder tcpMessageCoder = new();
    private TcpMessageQueue PrepareMessageQueue()
    {
        return new TcpMessageQueue(tcpMessageCoder);
    } 
    
    [Fact]
    public void SingleMessage()
    {
        var messageQueue = PrepareMessageQueue();

        var message = messageQueue.Dequeue();
        
        Assert.Null(message);

        var messageText = "MSG FROM tomas IS hi there\r\n";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));

        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Msg, message.MessageType);
    }
    
    [Fact]
    public void SinglePartialMessage()
    {
        var messageQueue = PrepareMessageQueue();

        var message = messageQueue.Dequeue();
        
        Assert.Null(message);

        var messageText = "MSG FROM to";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));
        
        message = messageQueue.Dequeue();
        Assert.Null(message);
        
        messageText = "mas IS hi ";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));
        
        message = messageQueue.Dequeue();
        Assert.Null(message);
        
        messageText = "there\r\n";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));

        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Msg, message.MessageType);
        Assert.Equal("hi there", message.Arguments[MessageArguments.MessageContent]);
    }
    
    [Fact]
    public void MultipleMessages()
    {
        var messageQueue = PrepareMessageQueue();

        var message = messageQueue.Dequeue();
        
        Assert.Null(message);

        var messageText = "MSG FROM tomas IS hi there\r\nJOIN channel AS tomas\r\n";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));

        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Msg, message.MessageType);
        Assert.Equal("hi there", message.Arguments[MessageArguments.MessageContent]);
        
        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Join, message.MessageType);
        Assert.Equal("channel", message.Arguments[MessageArguments.ChannelId]);
    }
    
    [Fact]
    public void MultiplePartialMessages()
    {
        var messageQueue = PrepareMessageQueue();

        var message = messageQueue.Dequeue();
        
        Assert.Null(message);

        var messageText = "MSG FROM tomas IS hi there\r\nJOIN cha";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));

        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Msg, message.MessageType);
        Assert.Equal("hi there", message.Arguments[MessageArguments.MessageContent]);
        
        messageText = "nnel AS tomas\r\nERR FROM tomas IS error\r\n";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));
        
        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Join, message.MessageType);
        Assert.Equal("channel", message.Arguments[MessageArguments.ChannelId]);
        Assert.Equal("tomas", message.Arguments[MessageArguments.DisplayName]);
        
        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Err, message.MessageType);
        Assert.Equal("tomas", message.Arguments[MessageArguments.DisplayName]);
        Assert.Equal("error", message.Arguments[MessageArguments.MessageContent]);
        
        message = messageQueue.Dequeue();
        
        Assert.Null(message);
    }
    
    [Fact]
    public void MultiplePartialMessagesWithInvalidMessage()
    {
        var messageQueue = PrepareMessageQueue();

        var message = messageQueue.Dequeue();
        
        Assert.Null(message);

        var messageText = "MSG FROM tomas IS hi there\r\nJOIN cha";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));

        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Msg, message.MessageType);
        Assert.Equal("hi there", message.Arguments[MessageArguments.MessageContent]);
        
        messageText = "nnel AS tomas\r\nERR FROM tomas I";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));
        
        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Join, message.MessageType);
        Assert.Equal("channel", message.Arguments[MessageArguments.ChannelId]);
        Assert.Equal("tomas", message.Arguments[MessageArguments.DisplayName]);

        messageText = "S error\r\ngtrgtrhrhtyhy";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));
        
        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Err, message.MessageType);
        Assert.Equal("tomas", message.Arguments[MessageArguments.DisplayName]);
        Assert.Equal("error", message.Arguments[MessageArguments.MessageContent]);
        
        message = messageQueue.Dequeue();
        Assert.Null(message);
        
        messageText = "\r\nBYE\r";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));
        
        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Unknown, message.MessageType);
        
        message = messageQueue.Dequeue();
        Assert.Null(message);
        
        messageText = "\n";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));
        
        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Bye, message.MessageType);
        
        message = messageQueue.Dequeue();
        
        Assert.Null(message);
    }
    
    [Fact]
    public void CLRFSeparated()
    {
        var messageQueue = PrepareMessageQueue();

        var message = messageQueue.Dequeue();
        
        Assert.Null(message);

        var messageText = "MSG FROM tomas IS hi\r";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));
        
        message = messageQueue.Dequeue();
        Assert.Null(message);
        
        messageText = "\nBYE\r\n";
        messageQueue.Enqueue(Encoding.UTF8.GetBytes(messageText));

        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Msg, message.MessageType);
        Assert.Equal("hi", message.Arguments[MessageArguments.MessageContent]);
        
        message = messageQueue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(MessageType.Bye, message.MessageType);
    }
}