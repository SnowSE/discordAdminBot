using Microsoft.AspNetCore.Components;

namespace Web.Components.Shared;

public abstract class LoadingChild : ComponentBase
{
  [CascadingParameter]
  protected LoadingBoundaryContext? Loading { get; set; }

  protected override async Task OnInitializedAsync()
  {
    using (Loading?.Start())
    {
      await Hydrate();
    }
  }

  protected virtual Task Hydrate()
  {
    return Task.CompletedTask;
  }
}
