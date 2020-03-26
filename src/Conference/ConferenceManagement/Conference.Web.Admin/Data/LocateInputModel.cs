using System;
using System.ComponentModel.DataAnnotations;

namespace Conference.Web.Admin.Data
{
    public class LocateInputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        public string AccessCode { get; set; }
    }
}