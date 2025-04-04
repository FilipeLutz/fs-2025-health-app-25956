using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HealthApp.Razor.Data;
using Microsoft.AspNetCore.Authorization;

namespace HealthApp.Razor.Pages
{
    [Authorize(Roles = "Admin")]
    public class DoctorPatientListModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DoctorPatientListModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<DoctorPatient> DoctorPatients { get; set; } = default!;

        public async Task OnGetAsync()
        {
            DoctorPatients = await _context.DoctorPatients
                .Include(d => d.Doctor)
                .Include(d => d.Patient).ToListAsync();
        }
    }
}