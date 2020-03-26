using System;
using System.ComponentModel.DataAnnotations;

namespace Conference.Admin.Share
{
    public class ConferenceEditInputModel
    {
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Description { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Location { get; set; }

        public string Tagline { get; set; }
        public string TwitterSearch { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy/MM/dd}")]
        [Display(Name = "Start")]
        public DateTime StartDate { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy/MM/dd}")]
        [Display(Name = "End")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Is Published?")]
        public bool IsPublished { get; set; }
        
        [Required(AllowEmptyStrings = false)]
        public string Slug { get; set; }
        
        [Required(AllowEmptyStrings = false)]
        public string AccessCode { get; set; }
    }
}