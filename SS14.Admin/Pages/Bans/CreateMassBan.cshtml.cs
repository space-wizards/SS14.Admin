using System.Globalization;
using Content.Server.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Bans
{
    [Authorize(Roles = "MASSBAN")]
    [ValidateAntiForgeryToken]
    public class CreateMassBanModel : PageModel
    {
        private readonly PostgresServerDbContext _dbContext;
        private readonly BanHelper _banHelper;
        private readonly IWebHostEnvironment _environment;

        public CreateMassBanModel(PostgresServerDbContext dbContext, BanHelper banHelper, IWebHostEnvironment environment)
        {
            _dbContext = dbContext;
            _banHelper = banHelper;
            _environment = environment;
        }

        public int BanCount { get; private set; }

        public class TsvEntry
        {
            public string UserId { get; set; }
            public string Address { get; set; }
            public string Hwid { get; set; }
            public string Reason { get; set; }
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
                    var ban = new ServerBan();

                    var error = await _banHelper.FillBanCommon(
                        ban,
                        entry.UserId,
                        entry.Address,
                        entry.Hwid,
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

            using (var csvReader = new CsvHelper.CsvReader(reader, config))
            {
                // Read the header
                csvReader.Read();
                csvReader.ReadHeader();

                // Read the records
                while (csvReader.Read())
                {
                    var record = new TsvEntry
                    {
                        UserId = csvReader.GetField<string>(0), // Assuming UserId is in the first column APPARENTLY ITS THE ONLY FUCKINKING COULM JASJDAJSDJASDJASJD
                        Address = csvReader.GetField<string>(1),
                        Hwid = csvReader.GetField<string>(2),
                        Reason = csvReader.GetField<string>(3)
                    };
                    records.Add(record);
                    //Assuming that the parse is vaid this sound return the correct amout of bans
                    BanCount += 1;
                }
            }

            return records;
        }

    }
}
