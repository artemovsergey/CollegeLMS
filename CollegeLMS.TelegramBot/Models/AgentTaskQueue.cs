using System.Collections.Concurrent;

namespace CollegeLMS.TelegramBot.Models;

public class AgentTaskQueue
{
    private readonly ConcurrentDictionary<Guid, AgentTask> _tasks = new();
    private readonly int _maxConcurrent;

    public AgentTaskQueue(int maxConcurrent = 3)
    {
        _maxConcurrent = maxConcurrent;
    }

    public AgentTask Enqueue(string prompt, long chatId, string messenger)
    {
        var task = new AgentTask
        {
            Prompt = prompt,
            ChatId = chatId,
            Messenger = messenger,
        };
        _tasks[task.Id] = task;
        return task;
    }

    public AgentTask? Get(Guid id) => _tasks.TryGetValue(id, out var task) ? task : null;

    public bool TryRemove(Guid id) => _tasks.TryRemove(id, out _);

    public int ActiveCount => _tasks.Values.Count(t => t.Status == AgentTaskStatus.Running);
}

