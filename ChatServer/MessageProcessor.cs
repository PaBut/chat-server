using ChatServer.Models;
using ChatServer.SocketClients;
using ChatServer.Utilities;

namespace ChatServer;

public class MessageProcessor : IMessageProcessor
{
    private const string ServerName = "Server";
    private const string DefaultChannelName = "Default";

    private readonly IAuthenticationService authenticationService;
    private readonly IChannelManager channelManager;
    private readonly WorkflowGraph workflowGraph = new WorkflowGraph();
    private readonly IIpkClient ipkClient;
    private readonly User user;

    private readonly MessageValidator messageValidator = new MessageValidator();

    public MessageProcessor(IAuthenticationService authenticationService, IChannelManager channelManager,
        IIpkClient ipkClient, User user)
    {
        this.authenticationService = authenticationService;
        this.channelManager = channelManager;
        this.ipkClient = ipkClient;
        this.user = user;
    }

    public async Task ProcessMessage(ResponseResult request, User user, CancellationToken cancellationToken = default)
    {
        if (request.ProcessingResult == ResponseProcessingResult.AlreadyProcessed ||
            request.Message.MessageType == MessageType.Confirm)
        {
            return;
        }

        if (request.ProcessingResult == ResponseProcessingResult.ParsingError ||
            request.Message.MessageType == MessageType.Unknown)
        {
            await SendErrorAndBye("Could not parse sent message", cancellationToken);
        }

        var message = request.Message;
        if (!workflowGraph.IsAllowedMessageType(message.MessageType))
        {
            await SendErrorAndBye("Invalid message for current state", cancellationToken);
            return;
        }
        
        switch (message.MessageType)
        {
            case MessageType.Auth:
                await ProcessAuthMessage(message, cancellationToken);
                break;
            case MessageType.Msg:
                await ProcessMsgMessage(message, cancellationToken);
                break;
            case MessageType.Join:
                await ProcessJoinMessage(message, cancellationToken);
                break;
            case MessageType.Bye:
                await ProcessByeMessage();
                break;
            case MessageType.Err:
                await ProcessErrMessage(message);
                break;
        }
    }

    public bool IsEndState => workflowGraph.IsEndState;

    private async Task ProcessErrMessage(Message message)
    {
        await ProcessByeMessage();
        await SendBye();
        workflowGraph.NextState(MessageType.Err, MessageType.Bye);
    }

    private async Task ProcessByeMessage()
    {
        await ChannelExit();
        workflowGraph.NextState(MessageType.Bye);
    }

    private async Task ChannelExit()
    {
        await ProcessMsgMessage(new Message()
        {
            MessageType = MessageType.Msg,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.DisplayName, ServerName },
                {
                    MessageArguments.MessageContent,
                    $"{user.DisplayName} has left the {user.Channel!.Name}"
                }
            }
        });
        channelManager.RemoveUserFromChannel(user, user.Channel.Name);
        user.Channel = null;
    }

    private async Task ChannelJoin(string channelId)
    {
        channelManager.AddUserToChannel(user, channelId);
        user.Channel = channelManager.GetChannel(channelId);

        await channelManager.SendToChannel(channelId, null, new Message()
        {
            MessageType = MessageType.Msg,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.DisplayName, ServerName },
                {
                    MessageArguments.MessageContent,
                    $"{user.DisplayName} has joined the {channelId}"
                }
            }
        });
    }

    private async Task ProcessJoinMessage(Message message, CancellationToken cancellationToken = default)
    {
        if (!messageValidator.IsValid(message))
        {
            await ipkClient.SendReply(false, "Sent join request is not valid", message, cancellationToken);
            workflowGraph.NextState(MessageType.Join, MessageType.Reply, false);
            return;
        }

        if (user.Channel != null)
        {
            await ChannelExit();
        }

        var channelId = (string)message.Arguments[MessageArguments.ChannelId];

        await ipkClient.SendReply(true, "Channel joined successfully", message, cancellationToken);

        await ChannelJoin(channelId);
        workflowGraph.NextState(MessageType.Join, MessageType.Reply, true);
    }

    private async Task ProcessMsgMessage(Message message, CancellationToken cancellationToken = default)
    {
        if (!messageValidator.IsValid(message))
        {
            await SendErrorAndBye("Sent message is not valid", cancellationToken);
            return;
        }

        await channelManager.SendToChannel(user.Channel!.Name, user.Username!, message);

        workflowGraph.NextState(MessageType.Msg);
    }

    private async Task ProcessAuthMessage(Message message, CancellationToken cancellationToken = default)
    {
        if (!messageValidator.IsValid(message))
        {
            await ipkClient.SendReply(false, "Sent authentication request is not valid", message, cancellationToken);
            workflowGraph.NextState(MessageType.Auth, MessageType.Reply, false);
            return;
        }

        var username = (string)message.Arguments[MessageArguments.UserName];

        if (channelManager.GetAllUsers().Select(u => u.Username).Contains(username))
        {
            await ipkClient.SendReply(false, "User with provided username is already authenticated", message,
                cancellationToken);
            workflowGraph.NextState(MessageType.Auth, MessageType.Reply, false);
            return;
        }

        var success = authenticationService.Authenticate(username,
            (string)message.Arguments[MessageArguments.Secret]);

        string messageContent;

        if (success)
        {
            messageContent = "Authentication successful";
        }
        else
        {
            messageContent = "Authentication failed";
        }

        await ipkClient.SendReply(success, messageContent, message, cancellationToken);


        if (success)
        {
            user.DisplayName = (string)message.Arguments[MessageArguments.DisplayName];
            user.Username = (string)message.Arguments[MessageArguments.UserName];

            await ChannelJoin(DefaultChannelName);
        }

        workflowGraph.NextState(MessageType.Auth, MessageType.Reply, success);
    }

    private async Task SendErrorAndBye(string errorText, CancellationToken cancellationToken = default)
    {
        await ipkClient.SendError(errorText, cancellationToken);
        await SendBye();
        workflowGraph.NextState(MessageType.Err, MessageType.Bye);
    }

    private async Task SendBye()
    {
        try
        {
            await ipkClient.Leave();
        }
        catch (NotReceivedConfirmException) { }
    }
}