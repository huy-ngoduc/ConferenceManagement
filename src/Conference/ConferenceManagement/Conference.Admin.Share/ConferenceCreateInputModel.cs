using System;
using System.ComponentModel.DataAnnotations;

namespace Conference.Admin.Share
{
    public class ConferenceCreateInputModel
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
        
        [Display(Name = "Owner")]
        [Required(AllowEmptyStrings = false)]
        public string OwnerName { get; set; }

        [Display(Name = "Email")]
        [Required(AllowEmptyStrings = false)]
        
        // TODO: move the message to resource file
        [RegularExpression(@"[\w-]+(\.?[\w-])*\@[\w-]+(\.[\w-]+)+", ErrorMessage = "The provided email address is not valid.")]
        public string OwnerEmail { get; set; }

        [Required(AllowEmptyStrings = false)]
        // TODO: move the message to resource file
        [RegularExpression(@"^\w+$", ErrorMessage = "The slug can only contain alphanumeric characters and underscores.")]
        public string Slug { get; set; }
    }
}