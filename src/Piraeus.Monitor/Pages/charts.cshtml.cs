using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Piraeus.Monitor
{
    public class ChartsModel : PageModel
    {
        public string ResourceUriString
        {
            get; internal set;
        }

        //private readonly HubAdapter adapter;

        public void OnGet(string r)
        {
            if (!string.IsNullOrEmpty(r))
            {
                ResourceUriString = r;
                //adapter.AddMetricObserverAsync(r).GetAwaiter();
            }
        }
    }
}