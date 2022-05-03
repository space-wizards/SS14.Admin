using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace SS14.Admin.AdminLogs;

public class LogJsonTagHelper : TagHelper
{
    private readonly PostgresServerDbContext _dbContext;
    public JsonDocument Json { get; set; } = default!;

    public LogJsonTagHelper(PostgresServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        if (Json.RootElement.ValueKind != JsonValueKind.Object) return;

        foreach (var entry in Json.RootElement.EnumerateObject())
        {
            var name = entry.Name;

            output.Content.AppendHtml("<div class=\"log-json-entry\">");

            if (entry.Value.ValueKind != JsonValueKind.Object)
            {
                output.Content.AppendHtml(BuildEntryTitle(name, "", entry.Value.GetRawText()));
                output.Content.AppendHtml("</div>");
                continue;
            }

            if (TryTranslateEntry(entry, out var result))
            {
                output.Content.AppendHtml(BuildEntryTitle(name, entry.Name));
                output.Content.AppendHtml(result);
                output.Content.AppendHtml("</div>");
                continue;
            }

            var content = string.Empty;

            foreach (var property in entry.Value.EnumerateObject())
            {
                if (property.Name.Equals("name"))
                {
                    name = property.Value.GetString() ?? name;
                    continue;
                }

                content += await TranslateProperty(property);
            }

            output.Content.AppendHtml(BuildEntryTitle(name, entry.Name));
            output.Content.AppendHtml(content);
            output.Content.AppendHtml("</div>");
        }
    }

    private bool TryTranslateEntry(JsonProperty entry, out string? result)
    {
        result = entry.Name switch
        {
            "coordinates" => ParsePosition(entry),
            "position" => ParsePosition(entry),
            _ => default
        };

        return result != default;
    }

    private async Task<string> TranslateProperty(JsonProperty property) => property.Name switch
    {
        "player" => await RetrievePlayerLink(property),
        _ => $"<p>{property.Name}: {property.Value.GetRawText() ?? "NULL"}</p>"
    };

    private string ParsePosition(JsonProperty property)
    {
        if (!property.Value.TryGetProperty("x", out var x) || !property.Value.TryGetProperty("y", out var y)) return "";
        return $"<p class=\"log-position\">X: {x} Y: {y}<p>";
    }

    //This will return a link to the players page in the future
    private async Task<string> RetrievePlayerLink(JsonProperty property)
    {
        if (!property.Value.TryGetGuid(out var playerId)) return string.Empty;

        var player = _dbContext.Player.FirstOrDefault(p => p.UserId.Equals(playerId));
        return player == default ? string.Empty : $"<a class=\"log-player-link\" href=\"#\">{player.LastSeenUserName}</a>";
    }

    private string BuildEntryTitle(string name, string type, string value = "")
    {
        var textInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
        return $"<h5 class=\"log-json-title\">{textInfo.ToTitleCase(name)} <span class=\"log-muted\">:{type}</span> {value}</h5>";
    }
}
