using Microsoft.AspNetCore.Mvc;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;

namespace StatusPageSharp.Web.Pages.Admin.Services;

public class EditModel(IAdminCatalogService adminCatalogService) : AdminPageModel
{
    [BindProperty]
    public ServiceUpsertModel Input { get; set; } = new();

    public IReadOnlyList<ServiceGroupAdminModel> Groups { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Groups = await adminCatalogService.GetServiceGroupsAsync(HttpContext.RequestAborted);
        var service = await adminCatalogService.GetServiceAsync(id, HttpContext.RequestAborted);
        if (service is null)
        {
            return NotFound();
        }

        Input = new ServiceUpsertModel
        {
            ServiceGroupId = service.ServiceGroupId,
            Name = service.Name,
            Slug = service.Slug,
            Description = service.Description,
            DisplayOrder = service.DisplayOrder,
            IsEnabled = service.IsEnabled,
            CheckPeriodSeconds = service.CheckPeriodSeconds,
            FailureThreshold = service.FailureThreshold,
            RecoveryThreshold = service.RecoveryThreshold,
            RawRetentionDaysOverride = service.RawRetentionDaysOverride,
            MonitorType = service.MonitorType,
            Host = service.Host,
            Port = service.Port,
            Url = service.Url,
            HttpMethod = service.HttpMethod,
            RequestHeadersJson = service.RequestHeadersJson,
            RequestBody = service.RequestBody,
            ExpectedStatusCodes = service.ExpectedStatusCodes,
            ExpectedResponseSubstring = service.ExpectedResponseSubstring,
            VerifyTlsCertificate = service.VerifyTlsCertificate,
            TimeoutSeconds = service.TimeoutSeconds,
        };

        ViewData["Title"] = $"Edit {service.Name}";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        Groups = await adminCatalogService.GetServiceGroupsAsync(HttpContext.RequestAborted);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await adminCatalogService.UpdateServiceAsync(id, Input, HttpContext.RequestAborted);
        return RedirectToPage("/Admin/Services/Index");
    }
}
