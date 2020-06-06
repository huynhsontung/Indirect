using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.User
{
    public class UserInfo : BaseUser
    {
        [JsonProperty("media_count")]
        public long MediaCount { get; set; }

        [JsonProperty("geo_media_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? GeoMediaCount { get; set; }

        [JsonProperty("follower_count")]
        public long FollowerCount { get; set; }

        [JsonProperty("following_count")]
        public long FollowingCount { get; set; }

        [JsonProperty("following_tag_count")]
        public long FollowingTagCount { get; set; }

        [JsonProperty("biography")]
        public string Biography { get; set; }

        [JsonProperty("can_link_entities_in_bio", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanLinkEntitiesInBio { get; set; }

        [JsonProperty("biography_with_entities")]
        public BiographyWithEntities BiographyWithEntities { get; set; }

        [JsonProperty("external_url")]
        public string ExternalUrl { get; set; }

        [JsonProperty("can_boost_post", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanBoostPost { get; set; }

        [JsonProperty("can_see_organic_insights", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanSeeOrganicInsights { get; set; }

        [JsonProperty("show_insights_terms", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ShowInsightsTerms { get; set; }

        [JsonProperty("can_convert_to_business", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanConvertToBusiness { get; set; }

        [JsonProperty("can_create_sponsor_tags", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanCreateSponsorTags { get; set; }

        [JsonProperty("can_create_standalone_fundraiser", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanCreateStandaloneFundraiser { get; set; }

        [JsonProperty("can_be_tagged_as_sponsor", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanBeTaggedAsSponsor { get; set; }

        [JsonProperty("can_see_support_inbox", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanSeeSupportInbox { get; set; }

        [JsonProperty("can_see_support_inbox_v1", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanSeeSupportInboxV1 { get; set; }

        [JsonProperty("total_igtv_videos")]
        public long TotalIgtvVideos { get; set; }

        [JsonProperty("total_clips_count")]
        public long TotalClipsCount { get; set; }

        [JsonProperty("total_ar_effects")]
        public long TotalArEffects { get; set; }

        [JsonProperty("reel_auto_archive", NullValueHandling = NullValueHandling.Ignore)]
        public string ReelAutoArchive { get; set; }

        [JsonProperty("is_profile_action_needed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsProfileActionNeeded { get; set; }

        [JsonProperty("usertags_count")]
        public long UsertagsCount { get; set; }

        [JsonProperty("usertag_review_enabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool? UsertagReviewEnabled { get; set; }

        [JsonProperty("is_needy", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsNeedy { get; set; }

        [JsonProperty("is_interest_account")]
        public bool IsInterestAccount { get; set; }

        [JsonProperty("has_chaining")]
        public bool HasChaining { get; set; }

        [JsonProperty("hd_profile_pic_versions")]
        public HdProfilePic[] HdProfilePicVersions { get; set; }

        [JsonProperty("hd_profile_pic_url_info")]
        public HdProfilePic HdProfilePicUrlInfo { get; set; }

        [JsonProperty("has_placed_orders", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasPlacedOrders { get; set; }

        [JsonProperty("can_tag_products_from_merchants", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanTagProductsFromMerchants { get; set; }

        [JsonProperty("show_conversion_edit_entry", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ShowConversionEditEntry { get; set; }

        [JsonProperty("aggregate_promote_engagement", NullValueHandling = NullValueHandling.Ignore)]
        public bool? AggregatePromoteEngagement { get; set; }

        [JsonProperty("allowed_commenter_type", NullValueHandling = NullValueHandling.Ignore)]
        public string AllowedCommenterType { get; set; }

        [JsonProperty("is_video_creator", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsVideoCreator { get; set; }

        [JsonProperty("has_profile_video_feed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasProfileVideoFeed { get; set; }

        [JsonProperty("has_highlight_reels")]
        public bool HasHighlightReels { get; set; }

        [JsonProperty("is_eligible_to_show_fb_cross_sharing_nux", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsEligibleToShowFbCrossSharingNux { get; set; }

        [JsonProperty("page_id_for_new_suma_biz_account")]
        public object PageIdForNewSumaBizAccount { get; set; }

        [JsonProperty("eligible_shopping_signup_entrypoints", NullValueHandling = NullValueHandling.Ignore)]
        public object[] EligibleShoppingSignupEntrypoints { get; set; }

        [JsonProperty("can_be_reported_as_fraud")]
        public bool CanBeReportedAsFraud { get; set; }

        [JsonProperty("is_business")]
        public bool IsBusiness { get; set; }

        [JsonProperty("account_type")]
        public long AccountType { get; set; }

        [JsonProperty("professional_conversion_suggested_account_type")]
        public long ProfessionalConversionSuggestedAccountType { get; set; }

        [JsonProperty("is_call_to_action_enabled")]
        public bool? IsCallToActionEnabled { get; set; }

        [JsonProperty("linked_fb_info", NullValueHandling = NullValueHandling.Ignore)]
        public LinkedFbInfo LinkedFbInfo { get; set; }

        [JsonProperty("fb_auto_xpost_settings", NullValueHandling = NullValueHandling.Ignore)]
        public FbAutoXpostSetting[] FbAutoXpostSettings { get; set; }

        [JsonProperty("can_see_primary_country_in_settings", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanSeePrimaryCountryInSettings { get; set; }

        [JsonProperty("personal_account_ads_page_name")]
        public string PersonalAccountAdsPageName { get; set; }

        [JsonProperty("personal_account_ads_page_id")]
        public long? PersonalAccountAdsPageId { get; set; }

        [JsonProperty("include_direct_blacklist_status")]
        public bool IncludeDirectBlacklistStatus { get; set; }

        [JsonProperty("can_follow_hashtag", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanFollowHashtag { get; set; }

        [JsonProperty("is_potential_business")]
        public bool IsPotentialBusiness { get; set; }

        [JsonProperty("show_post_insights_entry_point")]
        public bool ShowPostInsightsEntryPoint { get; set; }

        [JsonProperty("feed_post_reshare_disabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool? FeedPostReshareDisabled { get; set; }

        [JsonProperty("besties_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? BestiesCount { get; set; }

        [JsonProperty("show_besties_badge", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ShowBestiesBadge { get; set; }

        [JsonProperty("recently_bestied_by_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? RecentlyBestiedByCount { get; set; }

        [JsonProperty("nametag", NullValueHandling = NullValueHandling.Ignore)]
        public Nametag Nametag { get; set; }

        [JsonProperty("existing_user_age_collection_enabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ExistingUserAgeCollectionEnabled { get; set; }

        [JsonProperty("about_your_account_bloks_entrypoint_enabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool? AboutYourAccountBloksEntrypointEnabled { get; set; }

        [JsonProperty("auto_expand_chaining")]
        public bool AutoExpandChaining { get; set; }

        [JsonProperty("highlight_reshare_disabled")]
        public bool HighlightReshareDisabled { get; set; }

        [JsonProperty("is_memorialized")]
        public bool IsMemorialized { get; set; }

        [JsonProperty("open_external_url_with_in_app_browser")]
        public bool OpenExternalUrlWithInAppBrowser { get; set; }

        [JsonProperty("is_favorite", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsFavorite { get; set; }

        [JsonProperty("is_favorite_for_stories", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsFavoriteForStories { get; set; }

        [JsonProperty("is_favorite_for_igtv", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsFavoriteForIgtv { get; set; }

        [JsonProperty("is_favorite_for_highlights", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsFavoriteForHighlights { get; set; }

        [JsonProperty("live_subscription_status", NullValueHandling = NullValueHandling.Ignore)]
        public string LiveSubscriptionStatus { get; set; }

        [JsonProperty("mutual_followers_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? MutualFollowersCount { get; set; }

        [JsonProperty("profile_context", NullValueHandling = NullValueHandling.Ignore)]
        public string ProfileContext { get; set; }

        [JsonProperty("profile_context_links_with_user_ids", NullValueHandling = NullValueHandling.Ignore)]
        public ProfileContextLinksWithUserId[] ProfileContextLinksWithUserIds { get; set; }

        [JsonProperty("profile_context_mutual_follow_ids", NullValueHandling = NullValueHandling.Ignore)]
        public long[] ProfileContextMutualFollowIds { get; set; }

        [JsonProperty("show_shoppable_feed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ShowShoppableFeed { get; set; }

        [JsonProperty("shoppable_posts_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? ShoppablePostsCount { get; set; }

        [JsonProperty("merchant_checkout_style", NullValueHandling = NullValueHandling.Ignore)]
        public string MerchantCheckoutStyle { get; set; }

        [JsonProperty("is_eligible_for_smb_support_flow", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsEligibleForSmbSupportFlow { get; set; }

        [JsonProperty("displayed_action_button_partner")]
        public object DisplayedActionButtonPartner { get; set; }

        [JsonProperty("smb_donation_partner")]
        public object SmbDonationPartner { get; set; }

        [JsonProperty("smb_support_partner")]
        public object SmbSupportPartner { get; set; }

        [JsonProperty("smb_delivery_partner")]
        public object SmbDeliveryPartner { get; set; }

        [JsonProperty("smb_support_delivery_partner")]
        public object SmbSupportDeliveryPartner { get; set; }

        [JsonProperty("displayed_action_button_type", NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayedActionButtonType { get; set; }

        [JsonProperty("direct_messaging", NullValueHandling = NullValueHandling.Ignore)]
        public string DirectMessaging { get; set; }

        [JsonProperty("fb_page_call_to_action_id", NullValueHandling = NullValueHandling.Ignore)]
        public string FbPageCallToActionId { get; set; }

        [JsonProperty("address_street", NullValueHandling = NullValueHandling.Ignore)]
        public string AddressStreet { get; set; }

        [JsonProperty("business_contact_method", NullValueHandling = NullValueHandling.Ignore)]
        public string BusinessContactMethod { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("city_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? CityId { get; set; }

        [JsonProperty("city_name", NullValueHandling = NullValueHandling.Ignore)]
        public string CityName { get; set; }

        [JsonProperty("contact_phone_number", NullValueHandling = NullValueHandling.Ignore)]
        public string ContactPhoneNumber { get; set; }

        [JsonProperty("latitude", NullValueHandling = NullValueHandling.Ignore)]
        public double? Latitude { get; set; }

        [JsonProperty("longitude", NullValueHandling = NullValueHandling.Ignore)]
        public double? Longitude { get; set; }

        [JsonProperty("public_email", NullValueHandling = NullValueHandling.Ignore)]
        public string PublicEmail { get; set; }

        [JsonProperty("public_phone_country_code", NullValueHandling = NullValueHandling.Ignore)]
        public string PublicPhoneCountryCode { get; set; }

        [JsonProperty("public_phone_number", NullValueHandling = NullValueHandling.Ignore)]
        public string PublicPhoneNumber { get; set; }

        [JsonProperty("zip", NullValueHandling = NullValueHandling.Ignore)]
        public string Zip { get; set; }

        [JsonProperty("instagram_location_id", NullValueHandling = NullValueHandling.Ignore)]
        public string InstagramLocationId { get; set; }

        [JsonProperty("can_hide_category", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanHideCategory { get; set; }

        [JsonProperty("can_hide_public_contacts", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanHidePublicContacts { get; set; }

        [JsonProperty("should_show_category", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ShouldShowCategory { get; set; }

        [JsonProperty("should_show_public_contacts", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ShouldShowPublicContacts { get; set; }

        [JsonProperty("is_facebook_onboarded_charity", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsFacebookOnboardedCharity { get; set; }

        [JsonProperty("has_active_charity_business_profile_fundraiser", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasActiveCharityBusinessProfileFundraiser { get; set; }

        [JsonProperty("charity_profile_fundraiser_info", NullValueHandling = NullValueHandling.Ignore)]
        public CharityProfileFundraiserInfo CharityProfileFundraiserInfo { get; set; }

        [JsonProperty("is_bestie", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsBestie { get; set; }

        [JsonProperty("has_unseen_besties_media", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasUnseenBestiesMedia { get; set; }

        [JsonProperty("show_account_transparency_details", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ShowAccountTransparencyDetails { get; set; }

        [JsonProperty("show_leave_feedback", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ShowLeaveFeedback { get; set; }

        [JsonProperty("robi_feedback_source")]
        public object RobiFeedbackSource { get; set; }

        [JsonProperty("external_lynx_url", NullValueHandling = NullValueHandling.Ignore)]
        public Uri ExternalLynxUrl { get; set; }

        [JsonProperty("has_igtv_series", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasIgtvSeries { get; set; }
    }

    public partial class BiographyWithEntities
    {
        [JsonProperty("raw_text")]
        public string RawText { get; set; }

        [JsonProperty("entities")]
        public Entity[] Entities { get; set; }
    }

    public partial class Entity
    {
        [JsonProperty("hashtag", NullValueHandling = NullValueHandling.Ignore)]
        public Hashtag Hashtag { get; set; }

        [JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)]
        public EntityUser User { get; set; }
    }

    public partial class Hashtag
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public partial class EntityUser
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }
    }

    public partial class CharityProfileFundraiserInfo
    {
        [JsonProperty("pk")]
        public long Pk { get; set; }

        [JsonProperty("is_facebook_onboarded_charity")]
        public bool IsFacebookOnboardedCharity { get; set; }

        [JsonProperty("has_active_fundraiser")]
        public bool HasActiveFundraiser { get; set; }

        [JsonProperty("consumption_sheet_config")]
        public ConsumptionSheetConfig ConsumptionSheetConfig { get; set; }
    }

    public partial class ConsumptionSheetConfig
    {
        [JsonProperty("can_viewer_donate")]
        public bool CanViewerDonate { get; set; }

        [JsonProperty("currency")]
        public object Currency { get; set; }

        [JsonProperty("donation_url")]
        public object DonationUrl { get; set; }

        [JsonProperty("privacy_disclaimer")]
        public object PrivacyDisclaimer { get; set; }

        [JsonProperty("donation_disabled_message")]
        public string DonationDisabledMessage { get; set; }

        [JsonProperty("donation_amount_config")]
        public object DonationAmountConfig { get; set; }

        [JsonProperty("you_donated_message")]
        public object YouDonatedMessage { get; set; }
    }

    public partial class FbAutoXpostSetting
    {
        [JsonProperty("product_type")]
        public string ProductType { get; set; }

        [JsonProperty("setting_status")]
        public string SettingStatus { get; set; }

        [JsonProperty("setting_server_mtime")]
        public long SettingServerMtime { get; set; }
    }

    public partial class HdProfilePic
    {
        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("height")]
        public long Height { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }

    public partial class LinkedFbInfo
    {
        [JsonProperty("linked_fb_user")]
        public LinkedFbUser LinkedFbUser { get; set; }
    }

    public partial class LinkedFbUser
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("is_valid")]
        public bool IsValid { get; set; }
    }

    public partial class Nametag
    {
        [JsonProperty("mode")]
        public long Mode { get; set; }

        [JsonProperty("gradient")]
        public long Gradient { get; set; }

        [JsonProperty("emoji")]
        public string Emoji { get; set; }

        [JsonProperty("selfie_sticker")]
        public long SelfieSticker { get; set; }
    }

    public partial class ProfileContextLinksWithUserId
    {
        [JsonProperty("start")]
        public long Start { get; set; }

        [JsonProperty("end")]
        public long End { get; set; }

        [JsonProperty("username", NullValueHandling = NullValueHandling.Ignore)]
        public string Username { get; set; }
    }
}
