using Liongbot.Messaging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Liongbot.Dispatch {
    public class MessageReceivedEventArgs : EventArgs {
        /// <summary>
        /// Descriptive data that instructs the front end how and where to send
        /// a message back, or other information a front end required to work
        /// properly.
        /// </summary>
        public MessageMetadata Metadata { get; set; }
        public string RawMessage { get; set; }
    }

    public delegate void MessageReceivedEventHandler(
        IFrontEnd sender,
        MessageReceivedEventArgs args);

    public interface IFrontEnd {
        /// <summary>
        /// The GUID universally identifies the front end.
        /// </summary>
        Guid Identity { get; }

        /// <summary>
        /// Triggered when the front end received a message.
        /// </summary>
        event MessageReceivedEventHandler MessageReceived;

        /// <summary>
        /// Send a message with instruction encapsulated in `meta`.
        /// </summary>
        /// <param name="meta">Metadata describing how a message should be sent.
        /// </param>
        /// <param name="response">Message to be sent. Dispatcher ensures this
        /// parameter is nerver null.</param>
        /// <returns>Whether the message is sent successfully.</returns>
        bool Send(object meta, string response);

        /// <summary>
        /// Dispatcher has no back end announced processing of the
        /// message received, so the message is discarded.
        /// </summary>
        /// <param name="meta">Metadata describing how a message should be sent.
        /// </param>
        void Discard(object meta);
    }
}
