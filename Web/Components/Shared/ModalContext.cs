using Microsoft.AspNetCore.Components;

namespace Web.Components.Shared;

public sealed class ModalContext
{
  public Action? OnChange { get; set; }

  private RenderFragment? _content;
  public bool IsOpen => _content is not null;
  public RenderFragment? Content => _content;

  public void Show(RenderFragment content)
  {
    _content = content;
    OnChange?.Invoke();
  }

  public void Close()
  {
    _content = null;
    OnChange?.Invoke();
  }
}
