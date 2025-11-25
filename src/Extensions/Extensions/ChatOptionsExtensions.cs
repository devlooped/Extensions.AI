using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>Extensions for <see cref="ChatOptions"/>.</summary>
static partial class ChatOptionsExtensions
{
    extension(ChatOptions options)
    {
        /// <summary>Gets or sets the end user ID for the chat session.</summary>
        public string? EndUserId
        {
            get => (options.AdditionalProperties ??= []).TryGetValue("EndUserId", out var value) ? value as string : null;
            set => (options.AdditionalProperties ??= [])["EndUserId"] = value;
        }
    }
}