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
        private readonly PostgresServerDbContext _dbContext;
        private readonly BanHelper _banHelper;
        public ServerBanExemptFlags ExemptFlags;

        public CreateMassBanModel(PostgresServerDbContext dbContext, BanHelper banHelper, IWebHostEnvironment environment)
        {
            _dbContext = dbContext;
            _banHelper = banHelper;
        }

        public int BanCount { get; private set; }

        public class TsvEntry
        {
            public string UserId { get; set; }
            public string Address { get; set; }
            public string Hwid { get; set; }
            public string Reason { get; set; }
            public bool Datacenter { get; set; }
            public bool BlacklistedRange { get; set; }
        }

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
                    var ExemptFlags = BanExemptions.GetExemptionFromForm(Request.Form);

                    var ban = new ServerBan();

                    var ipAddr = entry.Address;
                    var hwid = entry.Hwid;

                    ban.ExemptFlags = ExemptFlags;
                    // ban.AutoDelete = Input.AutoDelete; // Could be added later
                    //ban.Hidden = Input.Hidden;
                    //ban.Severity = Input.Severity;

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

            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "\t", // Specify tab as the delimiter
                HasHeaderRecord = true, // TSV files have a header row
                MissingFieldFound = null // Ignore missing fields
            };

            using (var csvReader = new CsvReader(reader, config))
            {
                // Read the header
                csvReader.Read();
                csvReader.ReadHeader();

                // Read the records
                while (csvReader.Read())
                {
                    var record = new TsvEntry
                    {
                        UserId = csvReader.GetField<string>("user_id"),
                        Address = csvReader.GetField<string>("address"),
                        Hwid = csvReader.GetField<string>("hwid"),
                        Reason = csvReader.GetField<string>("reason"),
                        Datacenter = csvReader.GetField<bool>("datacenter"),
                        BlacklistedRange = csvReader.GetField<bool>("blacklisted_range")
                    };
                    records.Add(record);
                    BanCount += 1;
                }
            }

            return records;
        }
    }
}
