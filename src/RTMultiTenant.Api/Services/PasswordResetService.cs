using System.Collections.Concurrent;

namespace RTMultiTenant.Api.Services;

public class PasswordResetService
{
    private readonly ConcurrentDictionary<string, (Guid UserId, DateTime Expires)> _tokens = new();
    private readonly TimeSpan _ttl = TimeSpan.FromHours(2);

    public string Generate(Guid userId)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", string.Empty)
            .Replace("/", string.Empty)
            .Replace("=", string.Empty);
        _tokens[token] = (userId, DateTime.UtcNow.Add(_ttl));
        return token;
    }

    public Guid? Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        if (_tokens.TryGetValue(token, out var entry))
        {
            if (DateTime.UtcNow <= entry.Expires)
            {
                return entry.UserId;
            }
            _tokens.TryRemove(token, out _);
        }
        return null;
    }

    public void Invalidate(string token)
    {
        _tokens.TryRemove(token, out _);
    }
}

