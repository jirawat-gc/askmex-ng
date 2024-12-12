using Markdig;
using Microsoft.AspNetCore.Components;
using PTTGC.AskMeGc.BlazorCore;
using PTTGC.AskMeGc.OpenAI.Types;
using PTTGC.AskMeX.App.Core.Services;
using System.Text.RegularExpressions;

namespace PTTGC.AskMeX.App.Components;

public partial class ChatSession : IDisposable
{
    private MarkdownPipeline? pipeline;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        ChatSessionMediator.ChatPromptsChanged += InvokeStateHasChanged;
        ChatSessionMediator.StreamingResponseReceived += ChatSessionMediator_StreamingResponseReceived;
    }

    public void Dispose()
    {
        ChatSessionMediator.ChatPromptsChanged -= InvokeStateHasChanged;
        ChatSessionMediator.StreamingResponseReceived -= ChatSessionMediator_StreamingResponseReceived;
    }

    private async void InvokeStateHasChanged()
    {
        await InvokeAsync(StateHasChanged);
    }

    private void ChatSessionMediator_StreamingResponseReceived((string token, bool done) resp)
    {
        var typingText = TypingTexts.Refs.LastOrDefault();
        if (typingText != null)
        {
            typingText.PushToken(resp.token);
        }
    }

    private string DisplayFileName(string uri)
    {
        var uriObject = new Uri(uri);
        return Uri.UnescapeDataString(Path.GetFileName(uriObject.AbsolutePath));
    }

    /// <summary>
    /// Renders markdown to HTML
    /// </summary>
    /// <param name="markdown"></param> 
    /// <returns></returns>
    MarkupString RenderMarkdown(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return (MarkupString)string.Empty;
        }

        return (MarkupString)Markdig.Markdown.ToHtml(markdown, pipeline!);
    }

    [Inject]
    public required ChatSessionMediator ChatSessionMediator { private get; init; }

    private List<OpenAIChatMessage> ChatPrompts => ChatSessionMediator.ChatPrompts;

    private RefTracker<TypingText> TypingTexts { get; init; } = new();
}
