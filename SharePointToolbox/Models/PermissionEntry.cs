using System;
using System.Collections.Generic;
using System.Text;

namespace SharePointToolbox.Models
{
    public class PermissionEntry
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "Reader"; // Es: Read, Write, Owner
        public bool IsExternal { get; set; } = false;
        public bool IsGroup { get; set; }
    }
}
