using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SS14.Admin.Helpers
{
    public static class FormHiddenRoutes
    {
        public static IHtmlContent Make(Dictionary<string, string?> routeData, params string[] exclude)
        {
            var builder = new HtmlContentBuilder();
            foreach (var (k, v) in routeData)
            {
                if (exclude.Contains(k) || v == null)
                    continue;

                var tag = new TagBuilder("input");
                tag.Attributes.Add("type", "hidden");
                tag.Attributes.Add("name", k);
                tag.Attributes.Add("value", v);
                
                // ReSharper disable once MustUseReturnValue
                builder.AppendHtml(tag);
            }

            return builder;
        }
    }
}