using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiongbotPeripheral.GroupMe;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace StewardCielo.Controllers {
    public class MessageReceivedEventArgs : EventArgs {
        public IncomingMessage MessageContent;
    }
    public delegate void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs args);

    [Route("[controller]")]
    [ApiController]
    public class ReceiverController : ControllerBase {
        // POST api/values
        [HttpPost]
        public IActionResult ReceiveMessage([FromBody] IncomingMessage msg) {
            MessageReceived(this, new MessageReceivedEventArgs() {
                MessageContent = msg,
            });
            return Ok();
        }
        public static event MessageReceivedEventHandler MessageReceived;
    }
}
