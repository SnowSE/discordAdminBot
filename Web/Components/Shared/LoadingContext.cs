public class LoadingBoundaryContext
{
  private readonly Dictionary<Guid, bool> _loadingStates = new();

  public bool IsLoading => _loadingStates.Count > 0;

  public Action Start()
  {
    var id = Guid.NewGuid();
    _loadingStates[id] = true;
    OnChange?.Invoke();

    return () =>
    {
      _loadingStates.Remove(id);
      OnChange?.Invoke();
    };
  }

  public Action? OnChange { get; set; }
}
