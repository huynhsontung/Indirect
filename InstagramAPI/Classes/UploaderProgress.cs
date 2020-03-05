namespace InstagramAPI.Classes
{
    public class UploaderProgress
    {
        public InstaUploadState UploadState { get; internal set; }
 
        public string UploadId { get; internal set; }

        public string Caption { get; internal set; }

        public string Name { get; internal set; } = "Uploading single file";
    }

    public enum InstaUploadState
    {
        Preparing,
        Uploading,
        Uploaded,
        UploadingThumbnail,
        ThumbnailUploaded,
        Configuring,
        Configured,
        Completed,
        Error
    }
}