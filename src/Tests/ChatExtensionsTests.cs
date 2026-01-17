using System;
using System.Collections.Generic;
using System.Text;
using Devlooped.Extensions.AI;
using Devlooped.Extensions.AI.OpenAI;
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

    [Fact]
    public void ReasoningEffort_AutoSetsFactory()
    {
        var options = new ChatOptions();
        
        Assert.Null(options.RawRepresentationFactory);
        
        options.ReasoningEffort = ReasoningEffort.High;
        
        // Factory should now be auto-configured
        Assert.NotNull(options.RawRepresentationFactory);
        Assert.Equal(ReasoningEffort.High, options.ReasoningEffort);
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
    public void ReasoningEffortAndVerbosity_ShareFactory()
    {
        var options = new ChatOptions();
        
        options.ReasoningEffort = ReasoningEffort.Medium;
        var factory1 = options.RawRepresentationFactory;
        
        options.Verbosity = Verbosity.High;
        var factory2 = options.RawRepresentationFactory;
        
        // Factory should be the same - not replaced
        Assert.Same(factory1, factory2);
        
        Assert.Equal(ReasoningEffort.Medium, options.ReasoningEffort);
        Assert.Equal(Verbosity.High, options.Verbosity);
    }

    [Fact]
    public void ThrowsWhenCustomFactoryAlreadySet()
    {
        var options = new ChatOptions();
        
        // Set a custom factory first
        options.RawRepresentationFactory = _ => new object();
        
        // Should throw when trying to use extension properties
        Assert.Throws<InvalidOperationException>(() => options.ReasoningEffort = ReasoningEffort.High);
        Assert.Throws<InvalidOperationException>(() => options.Verbosity = Verbosity.Low);
    }

    [Fact]
    public void SettingNullDoesNotConfigureFactory()
    {
        var options = new ChatOptions();
        
        options.ReasoningEffort = null;
        
        // Factory should not be configured
        Assert.Null(options.RawRepresentationFactory);
    }

    [Fact]
    public void OpenAIChatOptions_BindingClass_Works()
    {
        var options = new OpenAIChatOptions
        {
            ReasoningEffort = ReasoningEffort.Low,
            Verbosity = Verbosity.Medium
        };
        
        Assert.Equal(ReasoningEffort.Low, options.ReasoningEffort);
        Assert.Equal(Verbosity.Medium, options.Verbosity);
        
        // Factory should be auto-configured via extension property setters
        Assert.NotNull(options.RawRepresentationFactory);
    }
}
