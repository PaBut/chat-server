using ChatServer.Models;

namespace ChatServer.Utilities;

public class WorkflowGraph
{
    private readonly IDictionary<(ClientState, (MessageType, bool?)), ClientState> stateMap =
        new Dictionary<(ClientState, (MessageType, bool?)), ClientState>()
        {
            { (ClientState.Start, (MessageType.Auth, null)), ClientState.Authentication },
            { (ClientState.Authentication, (MessageType.Auth, null)), ClientState.Authentication },
            { (ClientState.Authentication, (MessageType.Reply, true)), ClientState.Open },
            { (ClientState.Authentication, (MessageType.Reply, false)), ClientState.Authentication },
            { (ClientState.Authentication, (MessageType.Bye, null)), ClientState.End },
            { (ClientState.Open, (MessageType.Msg, null)), ClientState.Open },
            { (ClientState.Open, (MessageType.Join, null)), ClientState.Open },
            { (ClientState.Open, (MessageType.Reply, null)), ClientState.Open },
            { (ClientState.Open, (MessageType.Bye, null)), ClientState.End },
            { (ClientState.Error, (MessageType.Bye, null)), ClientState.End },
        };

    private readonly IDictionary<ClientState, MessageType[]?> allowedMessageTypes =
        new Dictionary<ClientState, MessageType[]?>()
        {
            { ClientState.Start, new[] { MessageType.Auth } },
            { ClientState.Authentication, new[] { MessageType.Reply, MessageType.Bye, MessageType.Auth } },
            { ClientState.Open, new[] { MessageType.Msg, MessageType.Join, MessageType.Bye, MessageType.Err } },
            { ClientState.Error, new[] { MessageType.Bye } },
            { ClientState.End, null }
        };
    
    private ClientState currentState;
    
    private readonly object locker;

    public WorkflowGraph()
    {
        currentState = ClientState.Start;
        locker = new();
    }

    public void NextState(MessageType messageType, bool? replySuccess = null)
    {
        lock (locker)
        {
            if (currentState == ClientState.End || messageType == MessageType.Confirm)
            {
                return;
            }

            var entry = (currentState, (messageType, replySuccess));
            (ClientState, (MessageType, bool?)) entryNull = (currentState, (messageType, null));

            if (stateMap.TryGetValue(entry, out var value))
            {
                currentState = value;
            }
            else if(stateMap.TryGetValue(entryNull, out var valueNull))
            {
                currentState = valueNull;
            }
            else
            {
                currentState = ClientState.Error;
            }
        }
    }

    public bool IsAllowedMessageType(MessageType messageType)
    {
        if (allowedMessageTypes[currentState] == null)
        {
            return false;
        }
        
        return allowedMessageTypes[currentState]!.Contains(messageType);
    }
    
    public void SetToErrorState()
    {
        currentState = ClientState.Error;
    }
    
    public ClientState CurrentState => currentState;
    
    public bool IsErrorState => currentState == ClientState.Error;
    public bool IsEndState => currentState == ClientState.End;
    public bool IsAuthenticated => currentState is ClientState.Open or ClientState.Error;
}