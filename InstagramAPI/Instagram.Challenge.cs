using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstagramAPI.Classes.Challenge;
using InstagramAPI.Classes.Core;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Utils;
using Newtonsoft.Json;

namespace InstagramAPI
{
    public partial class Instagram
    {
        public async Task<Result<ChallengeSelectMethodStep>> GetChallengeAsync()
        {
            if (ChallengeInfo == null || ChallengeInfo.Url == null ||
                string.IsNullOrEmpty(ChallengeInfo.ChallengeContext))
            {
                return Result<ChallengeSelectMethodStep>.Except(new Exception("ChallengeInfo not found."));
            }

            try
            {
                var requestUri = new UriBuilder(ChallengeInfo.Url)
                {
                    Query =
                        $"guid={Device.Uuid}" +
                        $"&device_id={Device.DeviceId}" +
                        $"&challenge_context={ChallengeInfo.ChallengeContext}"
                }.Uri;

                var response = await GetAsync(requestUri);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Result<ChallengeSelectMethodStep>.Fail(json, response.ReasonPhrase);
                }

                var stepResponse = JsonConvert.DeserializeObject<ChallengeStepResponse>(json);
                if (!stepResponse.IsOk())
                {
                    return Result<ChallengeSelectMethodStep>.Fail(stepResponse.StepData, json: json);
                }
                
                return Result<ChallengeSelectMethodStep>.Success(stepResponse.StepData);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception);
                return Result<ChallengeSelectMethodStep>.Except(exception);
            }
        }
    }
}
