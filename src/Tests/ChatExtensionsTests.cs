using System;
using System.Collections.Generic;
using System.Text;
using Devlooped.Extensions.AI;
using Devlooped.Extensions.AI.OpenAI;
using Microsoft.Extensions.AI;
using Moq;
using OpenAI.Responses;
using static Devlooped.Extensions.AI.Chat;

namespace Devlooped.Extensions.AI;

public class ChatExtensionsTests
{
    [Fact]
    public void ImplementsIList()
    {
        var first = User("hello");
        var second = Assistant("world");
        var replacement = Developer("updated");
        var chat = new Chat();
        IList<ChatMessage> list = chat;

        list.Add(first);
        list.Insert(1, second);

        Assert.Equal(2, chat.Count);
        Assert.False(chat.IsReadOnly);
        Assert.Same(first, chat[0]);
        Assert.Same(second, chat[1]);
        Assert.Contains(first, chat);
        Assert.Equal(1, chat.IndexOf(second));

        chat[1] = replacement;

        var copy = new ChatMessage[2];
        chat.CopyTo(copy, 0);

        Assert.Same(first, copy[0]);
        Assert.Same(replacement, copy[1]);

        chat.RemoveAt(0);
        Assert.True(chat.Remove(replacement));
        Assert.Empty(chat);
    }

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

    [Fact]
    public void Verbosity_AutoSetsFactory()
    {
        var options = new ChatOptions();

        Assert.Null(options.RawRepresentationFactory);

        options.Verbosity = Verbosity.Low;

        // Factory should now be auto-configured
        Assert.NotNull(options.RawRepresentationFactory);
        Assert.Equal(Verbosity.Low, options.Verbosity);
    }

    [Fact]
    public void ThrowsWhenCustomFactoryAlreadySet()
    {
        var options = new ChatOptions();

        // Set a custom factory first
        options.RawRepresentationFactory = _ => new object();

        // Should throw when trying to use extension properties
        Assert.Throws<InvalidOperationException>(() => options.Verbosity = Verbosity.Low);
    }

    [Fact]
    public void SettingNullDoesNotConfigureFactory()
    {
        var options = new ChatOptions();

        options.Verbosity = null;

        // Factory should not be configured
        Assert.Null(options.RawRepresentationFactory);
    }

    [Fact]
    public void OpenAIChatOptions_BindingClass_Works()
    {
        var options = new OpenAIChatOptions
        {
            Verbosity = Verbosity.Medium
        };

        Assert.Equal(Verbosity.Medium, options.Verbosity);

        // Factory should be auto-configured via extension property setters
        Assert.NotNull(options.RawRepresentationFactory);

        var responseOptions = options.RawRepresentationFactory(Mock.Of<IChatClient>());

        Assert.IsType<CreateResponseOptions>(responseOptions);
    }
}
