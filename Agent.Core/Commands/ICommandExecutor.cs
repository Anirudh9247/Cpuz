using Agent.Core.Models;

namespace Agent.Core.Commands;

public interface ICommandExecutor
{
    Task<CommandAckPayload> ExecuteCommandAsync(string commandId, string commandName, Dictionary<string, string> parameters, CancellationToken cancellationToken = default);
}
