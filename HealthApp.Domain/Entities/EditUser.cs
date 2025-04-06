using System;
using System.Collections.Generic;

namespace HealthApp.Domain.Entities
{
    public class EditUserViewModel
    {
        public string UserId { get; set; } 
        public string Name { get; set; } 
        public string Email { get; set; } 
        public List<RoleSelectionModel> Roles { get; set; } 
    }

    public class RoleSelectionModel
    {
        public string RoleName { get; set; }
        public bool IsSelected { get; set; } 
    }
}