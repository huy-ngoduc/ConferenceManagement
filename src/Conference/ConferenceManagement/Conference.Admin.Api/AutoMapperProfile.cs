using AutoMapper;
using Conference.Admin.Share;

namespace Conference.Admin.Api
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<EditableConferenceInfo, ConferenceInfo>();
            CreateMap<ConferenceCreateInputModel, ConferenceInfo>();
            CreateMap<ConferenceInfo, ConferenceReadModel>();
        }
    }
}