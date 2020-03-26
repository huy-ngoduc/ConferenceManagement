using System;

namespace Conference.Admin.Share
{
    public class ConferenceReadModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string Tagline { get; set; }
        public string TwitterSearch { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsPublished { get; set; }
        public Guid Id { get; set; }
        public string AccessCode { get; set; }
        public string OwnerName { get; set; }
        public string OwnerEmail { get; set; }
        public string Slug { get; set; }
        public bool WasEverPublished { get; set; }
    }
}