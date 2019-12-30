using System.Collections.Generic;
using InstaSharper.API;
using InstaSharper.Classes.Models.Media;
using InstaSharper.Classes.Models.User;
using InstaSharper.Enums;

namespace Indirect.Wrapper
{
    public class InstaCurrentUserWrapper : InstaUserShortWrapper
    {
        public bool HasAnonymousProfilePicture { get; set; }
        public string Biography { get; set; }
        public string ExternalUrl { get; set; }
        public List<InstaImage> HdProfileImages { get; set; }
        public InstaImage HdProfilePicture { get; set; }
        public bool ShowConversionEditEntry { get; set; }
        public string Birthday { get; set; }
        public string PhoneNumber { get; set; }
        public int CountryCode { get; set; }
        public long NationalNumber { get; set; }
        public InstaGenderType Gender { get; set; }
        public string Email { get; set; }

        public InstaCurrentUserWrapper(InstaCurrentUser source, IInstaApi api) : base(source, api)
        {
            HasAnonymousProfilePicture = source.HasAnonymousProfilePicture;
            Biography = source.Biography;
            ExternalUrl = source.ExternalUrl;
            HdProfileImages = source.HdProfileImages;
            HdProfilePicture = source.HdProfilePicture;
            ShowConversionEditEntry = source.ShowConversionEditEntry;
            Birthday = source.Birthday;
            PhoneNumber = source.PhoneNumber;
            CountryCode = source.CountryCode;
            NationalNumber = source.NationalNumber;
            Gender = source.Gender;
            Email = source.Email;

            ProfilePictureUrl = HdProfilePicture.Url;
        }
    }
}
