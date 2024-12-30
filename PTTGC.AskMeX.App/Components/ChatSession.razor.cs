using Markdig;
using Microsoft.AspNetCore.Components;
using PTTGC.AskMeGc.BlazorCore;
using PTTGC.AskMeGc.OpenAI;
using PTTGC.AskMeGc.OpenAI.Types;
using PTTGC.AskMeX.App.Core.Configurations;
using PTTGC.AskMeX.App.Core.Services;
using PTTGC.AskMeX.App.Core.Types;

namespace PTTGC.AskMeX.App.Components;

public partial class ChatSession : IDisposable
{
    private MarkdownPipeline? pipeline;
    private HashSet<string> supportRoles
        = new() { ChatPromptRoles.Assistant, ChatPromptRoles.User, CustomRole.AssistantReplyStatus };

    //protected override void OnAfterRender(bool firstRender)
    //{
    //    if (firstRender)
    //    {
    //    }
    //}

    protected override void OnInitialized()
    {
        base.OnInitialized();
        pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        Mediator.ChatSessionComponent = this;
        Mediator.ChatPromptsChanged += InvokeStateHasChanged;
        Mediator.StreamingResponseReceived += Mediator_StreamingResponseReceived;

#if DEBUG
        if (DebugSettings.IsTestingFilesSearchingState)
        {
            FilesSearchingState = FilesSearchingState.SearchingFiles;
        }
#endif
    }

    public void Dispose()
    {
        Mediator.ChatPromptsChanged -= InvokeStateHasChanged;
        Mediator.StreamingResponseReceived -= Mediator_StreamingResponseReceived;
    }

    public new void StateHasChanged()
    {
        base.StateHasChanged();
    }

    private async void InvokeStateHasChanged()
    {
        await InvokeAsync(StateHasChanged);
    }

    public void Mediator_StreamingResponseReceived((string token, bool done) resp)
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
    public required ChatSessionMediator Mediator { private get; init; }

    private IEnumerable<OpenAIChatMessage> ChatPrompts => Mediator.ChatPrompts.Where(cp => supportRoles.Contains(cp.Role));

    private RefTracker<TypingText> TypingTexts { get; init; } = new();

    public FilesSearchingState FilesSearchingState { get; set; }
}

/// <summary>
/// expected state flow would be
/// PendingAI -> DisplayingLatestStatus -> SearchingFiles -> ReplyingToUser
/// </summary>
public enum FilesSearchingState
{
    None,

    /// <summary>
    /// should only default status message with progress ring
    /// </summary>
    PendingAI,

    /// <summary>
    /// should only display the latest status with progress ring
    /// </summary>
    DisplayingLatestStatus,

    /// <summary>
    /// display the latest status with progress ring,
    /// and display cover image with magnifying glass
    /// </summary>
    SearchingFiles,

    /// <summary>
    /// hide cover image with magnifying glass,
    /// and display the latest status with progress ring,
    /// and display steaming message with references
    /// </summary>
    ReplyingToUser,
}