﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HumanRepProj.Pages;

public class ReportsModel : PageModel
{
    private readonly ILogger<ReportsModel> _logger;

    public ReportsModel(ILogger<ReportsModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}

