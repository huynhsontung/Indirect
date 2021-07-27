/*
* Referenced from Nerixyz/instagram_mqtt https://github.com/Nerixyz/instagram_mqtt
*/

struct GraphQLMessage {
  1: string topic,
  2: string payload,
}

struct SkywalkerMessage {
  1: i32 topic,
  2: string payload,
}
