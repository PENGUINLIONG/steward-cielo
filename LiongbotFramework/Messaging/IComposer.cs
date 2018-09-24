namespace Liongbot.Messaging {
    /// <summary>
    /// Interface for all frontend composer.
    /// </summary>
    public interface IComposer {
        /// <summary>
        /// Compose a message in a format that the frontend can understand.
        /// </summary>
        /// <param name="msg">Message to be composed.</param>
        /// <returns>Composed message text. Null is returned if and only if
        /// error occurred in procedure.</returns>
        string Compose(Message msg);

        /// <summary>
        /// Decompose a message from text info a compount message object.
        /// </summary>
        /// <param name="msg">Text message.</param>
        /// <returns>Decomposed compound message object. Null is returned if and
        /// only if error occurred in procedure.</returns>
        Compound Decompose(string msg);
    }
}
