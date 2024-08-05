using System.Globalization;
using Content.Server.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Admin.Helpers;
using CsvHelper;

namespace SS14.Admin.Pages.Bans
{
    [Authorize(Roles = "MASSBAN")]
    [ValidateAntiForgeryToken]
    public class CreateMassBanModel : PageModel
    {
        private static readonly CsvHelper.Configuration.CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture)
        {
            Delimiter = "\t", // Specify tab as the delimiter
            HasHeaderRecord = true, // TSV files have a header row
            MissingFieldFound = null // Ignore missing fields
        };

        private readonly PostgresServerDbContext _dbContext;
        private readonly BanHelper _banHelper;

        public CreateMassBanModel(PostgresServerDbContext dbContext, BanHelper banHelper)
        {
            _dbContext = dbContext;
            _banHelper = banHelper;
        }

        public int BanCount { get; private set; }

        public record TsvEntry(
            string? UserId,
            string? Address,
            string? Hwid,
            string Reason,
            bool Datacenter,
            bool BlacklistedRange
        );

        public async Task<IActionResult> OnPostAsync(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a file.");
                return Page();
            }

            if (!file.FileName.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Only TSV files are allowed.");
                return Page();
            }

            try
            {
                using var stream = new StreamReader(file.OpenReadStream());
                var entries = ParseTsv(stream);

                foreach (var entry in entries)
                {
                    var ban = new ServerBan();

                    var ipAddr = entry.Address;
                    var hwid = entry.Hwid;

                    if (entry.Datacenter)
                        ban.ExemptFlags |= ServerBanExemptFlags.Datacenter;
                    if (entry.BlacklistedRange)
                        ban.ExemptFlags |= ServerBanExemptFlags.BlacklistedRange;

                    var error = await _banHelper.FillBanCommon(
                        ban,
                        entry.UserId,
                        ipAddr,
                        hwid,
                        0, // Assuming lengthMinutes is always 0 for mass bans for now
                        entry.Reason);

                    if (error != null)
                    {
                        ModelState.AddModelError(string.Empty, error);
                        return Page();
                    }

                    _dbContext.Ban.Add(ban);
                }

                await _dbContext.SaveChangesAsync();

                TempData["StatusMessage"] = $"{entries.Count} ban(s) created successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                return Page();
            }
        }

        private List<TsvEntry> ParseTsv(StreamReader reader)
        {
            var records = new List<TsvEntry>();

            using var csvReader = new CsvReader(reader, CsvConfig);

            if (!csvReader.Read() || !csvReader.ReadHeader())
            {
                throw new InvalidDataException("The TSV file is missing a header.");
            }

            while (csvReader.Read())
            {
                var record = new TsvEntry(
                    csvReader.GetField<string>("user_id"),
                    csvReader.GetField<string>("address"),
                    csvReader.GetField<string>("hwid"),
                    csvReader.GetField<string>("reason") ?? "",
                    csvReader.GetField<bool>("datacenter"),
                    csvReader.GetField<bool>("blacklisted_range")
                );
                records.Add(record);
                BanCount += 1;
            }

            return records;
        }
    }
}
