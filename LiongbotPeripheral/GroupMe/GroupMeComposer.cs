using System;
using System.Collections.Generic;
using Liongbot.Messaging;
using Newtonsoft.Json;

namespace LiongbotPeripheral.GroupMe {
    public class GroupMeComposer : IComposer {
        public string Compose(Message msg) => msg.ToString();
        public Compound Decompose(string msg) => new Compound(msg);
    }
}
