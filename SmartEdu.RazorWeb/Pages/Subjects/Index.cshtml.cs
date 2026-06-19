using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.DTOs;

namespace SmartEdu.RazorWeb.Pages.Subjects
{
    public class IndexModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ISubjectService subjectService, ILogger<IndexModel> logger)
        {
            _subjectService = subjectService;
            _logger = logger;
        }

        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();

        // Simple bound properties for the Create form (using plain input names)
        [BindProperty]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string Description { get; set; } = string.Empty;

        [BindProperty]
        public SubjectUpdateDto EditSubject { get; set; } = new SubjectUpdateDto();

        public async Task OnGetAsync()
        {
            Subjects = await _subjectService.GetAllAsync();
            _logger.LogInformation("Loaded {Count} subjects", Subjects?.Count() ?? 0);
        }

        // Diagnostic: return raw subjects as JSON
        public async Task<IActionResult> OnGetRawAsync()
        {
            var list = await _subjectService.GetAllAsync();
            return new JsonResult(list);
        }

        // Debug helper: create a sample subject via GET for quick testing
        public async Task<IActionResult> OnGetCreateSampleAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Subjects = await _subjectService.GetAllAsync();
                return Page();
            }

            try
            {
                _logger.LogInformation("OnGetCreateSampleAsync creating subject: {Name}", name);
                await _subjectService.CreateAsync(new SubjectCreateDto { Name = name, Description = "Sample created via debug endpoint" });
                TempData["SuccessMessage"] = $"Sample subject '{name}' created.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sample subject");
                TempData["ErrorMessage"] = "Failed to create sample subject.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            // Log raw form values immediately for debugging
            _logger.LogInformation("Create form received - Name='{Name}', Description length={DescLen}", Request.Form["Name"].ToString(), Request.Form["Description"].ToString().Length);

            // Map simple fields into a DTO and validate it
            var dto = new SubjectCreateDto
            {
                Name = Name ?? Request.Form["Name"].ToString(),
                Description = Description ?? Request.Form["Description"].ToString()
            };

            if (!TryValidateModel(dto))
            {
                _logger.LogWarning("NewSubject validation failed when creating subject");
                foreach (var kv in ModelState)
                {
                    var key = kv.Key;
                    var errors = kv.Value.Errors.Select(e => e.ErrorMessage).ToArray();
                    if (errors.Length > 0)
                    {
                        _logger.LogWarning("ModelState[{Key}] errors: {Errors}", key, string.Join(";", errors));
                    }
                }

                var errorDict = ModelState.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.Errors.Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m)).ToArray()
                );

                Subjects = await _subjectService.GetAllAsync();
                // Return to page so validation messages are rendered; reopen create modal on client
                ViewData["OpenCreateModal"] = "true";
                return Page();
            }
            _logger.LogInformation("OnPostCreateAsync called. Name='{Name}', Description length={DescLen}", dto?.Name, dto?.Description?.Length ?? 0);
            try
            {
                await _subjectService.CreateAsync(dto);
                TempData["SuccessMessage"] = "Subject created successfully.";
                _logger.LogInformation("Subject '{Name}' created", dto?.Name);
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subject");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the subject.");
                Subjects = await _subjectService.GetAllAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            // Log incoming form for debugging
            _logger.LogInformation("Update form received. Form keys: {Keys}", string.Join(",", Request.Form.Keys.Cast<string>()));
            foreach (var k in Request.Form.Keys)
            {
                _logger.LogDebug("Form[{Key}] = {Value}", k, Request.Form[k]);
            }

            // Clear previous ModelState and bind EditSubject from the form values
            ModelState.Clear();
            var bound = await TryUpdateModelAsync(EditSubject, "EditSubject");
            _logger.LogInformation("TryUpdateModelAsync(EditSubject) returned {Bound}", bound);

            // Validate the DTO
            if (!TryValidateModel(EditSubject))
            {
                _logger.LogWarning("EditSubject validation failed");
                foreach (var kv in ModelState)
                {
                    var key = kv.Key;
                    var errors = kv.Value.Errors.Select(e => e.ErrorMessage).ToArray();
                    if (errors.Length > 0)
                    {
                        _logger.LogWarning("ModelState[{Key}] errors: {Errors}", key, string.Join(";", errors));
                    }
                }

                var errorDict = ModelState.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.Errors.Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m)).ToArray()
                );

                Subjects = await _subjectService.GetAllAsync();
                // Return to page so validation messages are rendered; reopen edit modal on client
                ViewData["OpenEditModal"] = "true";
                return Page();
            }

            try
            {
                await _subjectService.UpdateAsync(EditSubject);
                TempData["SuccessMessage"] = "Subject updated successfully.";
                _logger.LogInformation("Subject '{Id}' updated", EditSubject?.Id);
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subject {Id}", EditSubject?.Id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the subject.");
                Subjects = await _subjectService.GetAllAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _subjectService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Subject deleted.";
            return RedirectToPage();
        }
    }
}
