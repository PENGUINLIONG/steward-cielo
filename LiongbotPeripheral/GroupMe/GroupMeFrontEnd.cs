using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Liongbot.Dispatch;
using Liongbot.Messaging;
using Newtonsoft.Json;

namespace LiongbotPeripheral.GroupMe {
    [JsonObject]
    public class OutgoingMessage {
        [JsonProperty(PropertyName = "bot_id")]
        public string BotId;
        [JsonProperty(PropertyName = "text")]
        public string Text;
    }
    [JsonObject]
    public class IncomingMessage {
        [JsonProperty(PropertyName = "attachments")]
        public List<dynamic> Attachments;
        [JsonProperty(PropertyName = "avatar_url")]
        public string AvatarUrl;
        [JsonProperty(PropertyName = "created_at")]
        public long CreatedAt;
        [JsonProperty(PropertyName = "group_id")]
        public string GroupId;
        [JsonProperty(PropertyName = "id")]
        public string Id;
        [JsonProperty(PropertyName = "name")]
        public string Name;
        [JsonProperty(PropertyName = "sender_id")]
        public string SenderId;
        [JsonProperty(PropertyName = "sender_type")]
        // = "user"
        public string SenderType;
        [JsonProperty(PropertyName = "source_guid")]
        public string SourceGuid;
        [JsonProperty(PropertyName = "system")]
        // = false
        public bool System;
        [JsonProperty(PropertyName = "text")]
        public string Text;
        [JsonProperty(PropertyName = "user_id")]
        public string UserId;
    }
    public class GroupMeFrontEnd : IFrontEnd {
        public GroupMeFrontEnd(string botId) {
            _botId = botId;
        }

        public string _botId;
        public Guid Identity => new Guid("1B773246-E954-4E78-AA91-E9855AB8BC6F");
        public static HttpClient _client = new HttpClient();

        public event MessageReceivedEventHandler MessageReceived;

        public void AspCall(IncomingMessage json) {
            if (json.SenderType != "user") {
                return;
            }
            var meta = new MessageMetadata {
                UserId = json.SenderId,
                UserName = json.Name,
                GroupId = json.GroupId,
            };
            MessageReceived(this, new MessageReceivedEventArgs {
                Metadata = meta,
                RawMessage = json.Text,
            });
        }

        public void Discard(object meta) { }

        public bool Send(object meta, string response) {
            var text = new OutgoingMessage {
                BotId = _botId,
                Text = response,
            };
            var json = JsonConvert.SerializeObject(text);
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _client.PostAsync("https://api.groupme.com/v3/bots/post", content);
            return true;
        }
    }
}
