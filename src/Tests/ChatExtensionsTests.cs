using System;
using System.Collections.Generic;
using System.Text;
using Devlooped.Extensions.AI;
using Microsoft.Extensions.AI;
using static Devlooped.Extensions.AI.Chat;

namespace Devlooped;

public class ChatExtensionsTests
{
    [Fact]
    public void FactoryMethods()
    {
        var message = User("hello");

        Assert.Equal(ChatRole.User, message.Role);
        Assert.Equal("hello", message.Text);

        message = Assistant("hello");

        Assert.Equal(ChatRole.Assistant, message.Role);
        Assert.Equal("hello", message.Text);

        // Can't use System without qualifying since it's also a namespace.
        message = Chat.System("hello");
        Assert.Equal(ChatRole.System, message.Role);
        Assert.Equal("hello", message.Text);
    }
}
