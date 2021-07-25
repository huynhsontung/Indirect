/*
* Reference from Valga/Fbns-react https://github.com/valga/fbns-react
* and Nerixyz/instagram_mqtt https://github.com/Nerixyz/instagram_mqtt
*/

struct ConnectPayload {
  1: string clientId,
  2: string willTopic,
  3: string willMessage,
  4: ClientInfo clientInfo,
  5: string password,
  6: list<string> diffRequests,
  9: string zeroRatingTokenHash,
  10: map<string,string> appSpecificInfo
}

struct ClientInfo {
  1: i64 userId,
  2: string userAgent,
  3: i64 clientCapabilities,
  4: i64 endpointCapabilities,
  5: i32 publishFormat,
  6: bool noAutomaticForeground,
  7: bool makeUserAvailableInForeground,
  8: string deviceId,
  9: bool isInitiallyForeground,
  10: i32 networkType,
  11: i32 networkSubtype,
  12: i64 clientMqttSessionId,
  13: string clientIpAddress,
  14: list<i32> subscribeTopics,
  15: string clientType,
  16: i64 appId,
  17: bool overrideNectarLogging,
  18: string connectTokenHash,
  19: string regionPreference,
  20: string deviceSecret,
  21: i8 clientStack,
  22: i64 fbnsConnectionKey,
  23: string fbnsConnectionSecret,
  24: string fbnsDeviceId,
  25: string fbnsDeviceSecret
}
