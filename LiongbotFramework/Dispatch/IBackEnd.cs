using Liongbot.Messaging;
using System;

namespace Liongbot.Dispatch {
    public class BackEndMetadata {
        public Guid Identity { get; set; }
        public string FriendlyName { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
    }

    public interface IBackEnd {
        BackEndMetadata Metadata { get; }
        /// <summary>
        /// Indicates that the back end processes raw message.
        /// </summary>
        bool AcceptRaw { get; }
        /// <summary>
        /// Indicates that the back end is designed with thread safety
        /// considered. The Dispatcher will invoke the back end asynchronously.
        /// </summary>
        bool ThreadSafe{ get; }

        /// <summary>
        /// Launch the plugin and allocate all resources needed.
        /// </summary>
        /// <param name="context">Runtime context.</param>
        /// <returns>True if the plugin is launched successfully; false
        /// otherwise.</returns>
        bool Launch();
        /// <summary>
        /// Shut down the plugin and release all resources. This method MUST NOT
        /// fail.
        /// </summary>
        /// <param name="context">Runtime context.</param>
        void ShutDown();

        /// <summary>
        /// Have a rapid scan of the incoming message and check if the current
        /// plugin is responding to the message.
        /// 
        /// If an implementation is cannot decide in a reasonably short period
        /// of time, it can return true right away.
        /// </summary>
        /// <returns>True if the plugin is going to process the message; false
        /// otherwise.</returns>
        bool Preview(IncomingMessage msg);

        /// <summary>
        /// Respond to incoming message.
        /// </summary>
        /// <param name="msg">Incoming message to be processed.</param>
        /// <param name="response"></param>
        /// <returns>Response message; null if the back end gave up.</returns>
        bool Respond(IncomingMessage msg, out Message res);
    }
}
