using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using SS14.Admin.AdminLogs.Export;

namespace SS14.Admin.Pages.Logs;

public class Export : PageModel
{
    public const int MaxDays = 4;
    public const string FromAfterToError = "From date needs to be before to date";
    public const string DateRangeToLargeError = "Selected date range larger than 7 days";
    public const string RoundIdMissingError = "Round id is required";

    private readonly LogExportQueue _queue;
    private readonly IOptions<ExportConfiguration> _config;

    [BindProperty, DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime FromDate { get; set; } = DateTime.Now.AddDays(-1);

    [BindProperty, DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime ToDate { get; set; } = DateTime.Now;

    [BindProperty]
    public bool UseRoundId { get; set; }

    [BindProperty]
    public int? RoundId { get; set; }

    [BindProperty]
    public string? SearchText { get; set; }

    [BindProperty]
    public bool UseCompression { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public bool Processing { get; set; }

    public Export(LogExportQueue queue, IOptions<ExportConfiguration> config)
    {
        _queue = queue;
        _config = config;
    }

    public void OnGet()
    {

    }

    public async Task OnPost(CancellationToken ct)
    {
        switch (UseRoundId)
        {
            case false when FromDate > ToDate:
                ErrorMessage = FromAfterToError;
                return;
            case false when (ToDate - FromDate).TotalDays > MaxDays:
                ErrorMessage = DateRangeToLargeError;
                return;
            case true when !RoundId.HasValue:
                ErrorMessage = RoundIdMissingError;
                break;
        }

        DateTime? from = !UseRoundId ? FromDate : null;
        DateTime? to = !UseRoundId ? ToDate : null;
        var roundId = UseRoundId ? RoundId : null;

        var item = new ExportProcessItem(SearchText, from, to, roundId, UseCompression);
        await _queue.TryQueueProcessItem(item);
        Processing = true;
    }

    public List<string?> ListExportedFiles()
    {
        var exportPath = Path.Combine(Directory.GetCurrentDirectory(), _config.Value.ExportDirectory);
        var files = Directory.EnumerateFiles(exportPath, "*.csv*");

        return files.Select(Path.GetFileName).ToList();
    }
}
