using System.Collections.Generic;

namespace InstagramAPI.Classes.Media
{
    public class InstaVideoUpload
    {
        public InstaVideoUpload(InstaVideo video, InstaImage videoThumbnail)
        {
            Video = video;
            VideoThumbnail = videoThumbnail;
        }

        public InstaVideo Video { get; set; }

        public InstaImage VideoThumbnail { get; set; }

        /// <summary>
        ///     User tags => Optional
        /// </summary>
        public List<UserTagVideoUpload> UserTags { get; set; } = new List<UserTagVideoUpload>(0);
    }

    public class UserTagVideoUpload
    {
        public string Username { get; set; }

        public long Pk { get; set; } = -1;
    }
}