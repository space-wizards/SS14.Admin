using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Settings;

public class Index : PageModel
{
    public List<ChangeEntry> Changes { get; set; }
    private readonly IWebHostEnvironment _env;
    private readonly YamlHelper _yamlHelper;
    public Index(IWebHostEnvironment env, YamlHelper yamlHelper)
    {
        _env = env;
        _yamlHelper = yamlHelper;
    }
    public void OnGet()
    {
        var filepath = Path.Combine(_env.ContentRootPath, "changelog.yml");
        Changes = _yamlHelper.ReadYamlFile(filepath);
    }


    //gets the icons for the CL
    public string GetIcon(string type)
    {
        switch (type)
        {
            case "Add":
                return "fas fa-plus";
            case "Remove":
                return "fas fa-minus";
            case "Tweak":
                return "fas fa-wrench";
            case "Fix":
                return "fas fa-bug";
        }

        return "fas fa-wrench";
    }
}
