using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Conference.Admin.Share
{
    public class ConferenceIdentity
    {
        [Required]
        public string Slug { get; set; }
        
        [Required]
        public string AccessCode { get; set; }
    }
}
