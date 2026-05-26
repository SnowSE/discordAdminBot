public class LoadingBoundaryContext
{
  private readonly Dictionary<Guid, bool> _loadingStates = new();

  public bool IsLoading => _loadingStates.Count > 0;

  public IDisposable Start(Func<Task>? onComplete = null)
  {
    var id = Guid.NewGuid();
    _loadingStates[id] = true;
    OnChange?.Invoke();

    return new LoadingScope(this, id,   onComplete);
  }

  private void Complete(Guid id)
  {
    _loadingStates.Remove(id);
    OnChange?.Invoke();
  }

  private sealed class LoadingScope : IDisposable
  {
    private readonly LoadingBoundaryContext _context;
    private readonly Guid _id;
    private readonly Func<Task>? _onComplete;

    public LoadingScope(LoadingBoundaryContext context, Guid id, Func<Task>? onComplete = null)
    {
      _context = context;
      _id = id;
      _onComplete = onComplete;
    }

    public void Dispose()
    {
      _context.Complete(_id);
      _onComplete?.Invoke();
    }
  }

  public Action? OnChange { get; set; }
}
