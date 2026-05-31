namespace AH.TestU4IDS.NET10.Web.Services;

/// <summary>
/// Service for managing application-wide errors.
/// </summary>
public class ErrorService
{
    private string? _errorMessage;
    private readonly List<Action> _subscribers = [];

    /// <summary>
    /// Gets the current error message, if any.
    /// </summary>
    public string? ErrorMessage => _errorMessage;

    /// <summary>
    /// Sets an error message and notifies all subscribers.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    public void SetError(string message)
    {
        _errorMessage = message;
        NotifySubscribers();
    }

    /// <summary>
    /// Clears the error message and notifies all subscribers.
    /// </summary>
    public void ClearError()
    {
        _errorMessage = null;
        NotifySubscribers();
    }

    /// <summary>
    /// Subscribes to error changes. The callback is invoked whenever an error is set or cleared.
    /// </summary>
    /// <param name="callback">The callback to invoke when errors change.</param>
    /// <returns>An action to unsubscribe.</returns>
    public Action Subscribe(Action callback)
    {
        _subscribers.Add(callback);
        return () => _subscribers.Remove(callback);
    }

    private void NotifySubscribers()
    {
        foreach (var subscriber in _subscribers)
        {
            subscriber.Invoke();
        }
    }
}
