using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using PTTGC.AskMeGc;
using PTTGC.AskMeGc.BlazorCore;
using PTTGC.AskMeGc.Feedback;
using PTTGC.AskMeGc.OpenAI;
using PTTGC.AskMeGc.OpenAI.Types;
using PTTGC.AskMeX.App.Core.Types;

namespace PTTGC.AskMeX.App.Core.ObjectModels;

// NOTE [YO] : has not been using this class as this code was duplicated from AskMeGC Project
// might need to remove this class when clean up
public abstract class AskMeGCPage<T> : ComponentBase, IAsyncDisposable where T : OpenAIChatSession, new()
{
    /// <summary>
    /// Kind of this session, must be overridden to specify history entry prefix
    /// </summary>
    public abstract string SessionKind { get; }

    /// <summary>
    /// Url Prefix for this page
    /// </summary>
    public abstract string UrlPrefix { get; }

    /// <summary>
    /// List of script to import
    /// </summary>
    protected Dictionary<string, IJSObjectReference> _JSModules = new();

    [Parameter]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    [Inject]
    public ILocalStorageService LocalStorage { get; set; }

    [Inject]
    public NavigationManager NavigationManager { get; set; }

    [Inject]
    public IAccessTokenProvider TokenProvider { get; set; }

    [Inject]
    public AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    [Inject]
    public IJSRuntime JS { get; set; }

    public bool IsBusy { get; set; }

    private AccessToken AccessToken { get; set; }

    public virtual T ChatSession { get; private set; } = new();

    public string Helper_HideIfNewSession => this.ChatSession.IsSessionBegan ? "" : "hidden";
    public string Helper_ShowIfNewSession => this.ChatSession.IsSessionBegan ? "hidden" : "";
    public string Helper_ShowIfBusy => this.IsBusy ? "" : "hidden";
    public string Helper_DisableIfBusy => this.IsBusy ? "disabled" : "";

    /// <summary>
    /// Current user prompt for binding
    /// </summary>
    public string CurrentUserPrompt { get; set; }

    /// <summary>
    /// Progress
    /// </summary>
    public ProgressReport CurrentProgress { get; set; } = new();

    private string GetSessionStorageKey(string sessionId)
    {
        return $"{this.SessionKind}-{sessionId}";
    }

    public string Helper_HideIf(bool condition)
    {
        return condition ? "hidden" : "";
    }

    public string Helper_ShowIf(bool condition)
    {
        return condition ? "" : "hidden";
    }

    /// <summary>
    /// Available Search Session that was saved. Assign Value to have the list persisted in local storage
    /// </summary>
    public List<SessionListItem> AvailableSession
    {
        get
        {
            var chats = this.LocalStorage.GetItem<List<SessionListItem>>($"{this.SessionKind}-sessions");
            if (chats == null)
            {
                return new List<SessionListItem>();
            }

            return chats;
        }
        set
        {
            this.LocalStorage.SetItem($"{this.SessionKind}-sessions", value);
        }
    }

    private async void OnLocationChanged(object? s, LocationChangedEventArgs e)
    {
        if (e.Location.Contains(this.UrlPrefix) == false)
        {
            return;
        }

        await this.LoadPage();
    }

    protected override void OnInitialized()
    {
        this.NavigationManager.LocationChanged += this.OnLocationChanged;
    }

    protected override Task OnParametersSetAsync()
    {
        return this.LoadPage();
    }

    /// <summary>
    /// Called before page loading code is ran, access token is not available and no navigation was performed
    /// </summary>
    /// <returns></returns>
    protected virtual Task OnPageLoading() => Task.CompletedTask;

    /// <summary>
    /// Called after page loading is done and access token is ready
    /// </summary>
    /// <returns></returns>
    protected virtual Task OnPageLoaded() => Task.CompletedTask;

    private string _LastUrl = null;

    private async Task LoadPage()
    {

        await this.OnPageLoading();

        if (string.IsNullOrEmpty(this.SessionId) == false)
        {
            if (this.AvailableSession.Any(c => c.Id == this.SessionId) == false)
            {
                this.NavigationManager.NavigateTo(this.UrlPrefix);
                return;
            }

            var json = this.LocalStorage.GetItem<string>(this.GetSessionStorageKey(this.SessionId));
            this.ChatSession = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            if (this.ChatSession == null)
            {
                this.NavigationManager.NavigateTo(this.UrlPrefix);
                return;
            }

            this.ChatSession.Client = GCOpenAIPlatform.Instance;
            foreach (var message in this.ChatSession.ChatPrompts.Where(m => m.Role == "assistant"))
            {
                await this.OnMessageLoaded(message);
                await this.RenderMarkdown(message);
            }

            await this.OnSessionLoaded();

            await this.ScrollToBottom();
        }
        else
        {
            // this is a new session
            this.ChatSession = await this.OnSessionCreating();
            await this.OnSessionCreated();
        }

        this.ChatSession.ChatPromptsChanged += async () =>
        {
            foreach (var message in this.ChatSession.ChatPrompts.ToList().Where(m => m.Role == "assistant"))
            {
                if (string.IsNullOrEmpty(message.RenderedContent))
                {
                    await this.RenderMarkdown(message);
                }
            }

            _ = this.InvokeAsync(this.StateHasChanged);
            this.StateHasChanged();
        };

        this.ChatSession.StateChanged += () =>
        {
            _ = this.InvokeAsync(this.StateHasChanged);
        };

        if (await this.AuthenticationStateProvider.IsInRole("askmegc-user") == false)
        {
            this.NavigationManager.NavigateTo("/");
            return;
        }

        if (await this.RefreshToken() == false)
        {
            return;
        }

        GCOpenAIPlatform.Instance.UserToken = this.AccessToken.Value;
        GCOpenAIPlatform.Instance.CurrentSessionId = this.ChatSession.SessionId;
        GCOpenAIPlatform.Instance.AppId = this.SessionKind;

        foreach (var script in _JSModules.Keys)
        {
            _JSModules[script] = await JS.InvokeAsync<IJSObjectReference>("import", script);
        }

        await this.OnPageLoaded();

        FeedbackContext.LastUserPrompt = this.ChatSession?.ChatPrompts?.LastOrDefault(c => c.Role == ChatPromptRoles.User)?.Content ?? string.Empty;
        FeedbackContext.LastModelResponse = this.ChatSession?.ChatPrompts?.LastOrDefault(c => c.Role == ChatPromptRoles.Assistant)?.Content ?? string.Empty;

        this.IsBusy = false;
        this.StateHasChanged();

        _ = JS.InvokeVoidAsync("scrollToSelector", "header");

    }

    protected async Task<bool> RefreshToken()
    {
        if (this.AccessToken == null ||
            this.AccessToken?.Expires > DateTimeOffset.Now.AddMinutes(-1))
        {
            try
            {
                var tokenResult = await TokenProvider.RequestAccessToken();
                AccessToken token;
                if (tokenResult.TryGetToken(out token) == false)
                {
                    this.NavigationManager.NavigateToLogout("/authentication/logout");
                    return false;
                }

                if (token == null ||
                    token.Expires < DateTimeOffset.Now.AddMinutes(-1))
                {
                    this.NavigationManager.NavigateToLogout("/authentication/logout");
                    return false;
                }

                this.AccessToken = token;
            }
            catch (Exception)
            {
                this.NavigationManager.NavigateToLogout("/authentication/logout");
            }
        }

        return true;
    }

    protected virtual Task OnSessionLoaded()
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnMessageLoaded(OpenAIChatMessage message)
    {
        return Task.CompletedTask;
    }

    protected abstract Task<T> OnSessionCreating();

    protected virtual Task OnSessionCreated()
    {
        return Task.CompletedTask;
    }

    public void SetBusyState(bool busy)
    {
        this.IsBusy = busy;
        this.StateHasChanged();
    }


    public void SaveSession()
    {
        var allExceptThis = this.AvailableSession.Where(c => c.Id != this.ChatSession.SessionId);

        var firstQuestion = this.ChatSession.ChatPrompts.FirstOrDefault(c => c.Role == ChatPromptRoles.User);
        if (firstQuestion == null)
        {
            return;
        }

        this.ChatSession.SessionName = firstQuestion.ShortContent ?? firstQuestion.Content;

        this.AvailableSession = allExceptThis.Concat(new[] {
            new SessionListItem()
            {
                Id =this.ChatSession.SessionId,
                Title = this.ChatSession.SessionName,
                Updated = DateTime.Now

            }}).ToList();

        var json = JsonConvert.SerializeObject(this.ChatSession);
        this.LocalStorage.SetItem(this.GetSessionStorageKey(this.ChatSession.SessionId), json);

        this.StateHasChanged();
    }

    public async Task DeleteSession(SessionListItem item)
    {
        var answer = await this.JS.InvokeAsync<bool>("confirm", $"Do you want to delete conversation about '{item.Title}'?");
        if (answer == false)
        {
            return;
        }

        this.AvailableSession = this.AvailableSession.Where(s => s.Id != item.Id).ToList();
        this.StateHasChanged();
    }

    public void NewSession()
    {
        this.NavigationManager.NavigateTo(this.UrlPrefix);
    }

    public void HandleEnter(KeyboardEventArgs e)
    {
        if (e.ShiftKey && (e.Code == "Enter" || e.Code == "NumpadEnter"))
        {
            this.SubmitPrompt();
        }
    }

    private async Task RenderMarkdown(OpenAIChatMessage message)
    {
        message.RenderedContent = await JS.InvokeAsync<string>("renderMarkdown", message.Content);
    }

    protected virtual async Task OnSubmittingPrompt(string userPrompt)
    {
        await this.ChatSession.AddChatPrompt(userPrompt, 1000);
    }

    public Task SubmitPrompt()
    {
        return this.SubmitPrompt(this.CurrentUserPrompt);
    }

    public async Task SubmitPrompt(string prompt)
    {
        bool wasError = false;

        if (prompt == null)
        {
            prompt = this.CurrentUserPrompt;
        }

        if (string.IsNullOrEmpty(prompt))
        {
            return;
        }

        if (await this.RefreshToken() == false)
        {
            return;
        }

        this.SetBusyState(true);

        try
        {
            await this.OnSubmittingPrompt(prompt);
        }
        catch (Exception ex)
        {
            this.ChatSession.ChatPrompts.Add(new OpenAIChatMessage()
            {
                Role = ChatPromptRoles.System,
                Content = "Cannot process your request:" + ex.Message,
                IsExcludedFromContext = true,
                //Session = this.ChatSession
            });

            wasError = true;
            this.SetBusyState(false);
        }

        var lastMessage = this.ChatSession.ChatPrompts.LastOrDefault(m => m.Role == "assistant");
        if (lastMessage != null)
        {
            if (string.IsNullOrEmpty(lastMessage.Content))
            {
                this.ChatSession.ChatPrompts.Remove(lastMessage);
                this.ChatSession.StreamingMessage.IsStreaming = false;
            }
            else
            {
                await this.RenderMarkdown(lastMessage);

                FeedbackContext.LastUserPrompt = prompt;
                FeedbackContext.LastModelResponse = lastMessage.Content;
            }

        }

        this.SetBusyState(false);
        this.SaveSession();

        this.CurrentUserPrompt = string.Empty;

        this.SetBusyState(false);
        this.StateHasChanged();
    }

    protected ValueTask ScrollToBottom()
    {
        return JS.InvokeVoidAsync("scrollBottom");
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        foreach (var module in _JSModules.Values)
        {
            await module.DisposeAsync();
        }

        this.NavigationManager.LocationChanged -= this.OnLocationChanged;
    }

    public ValueTask SaveFile(string filename, byte[] data)
    {
        return this.JS.InvokeVoidAsync(
            "window.saveAsFile",
            filename,
            Convert.ToBase64String(data));
    }

    protected async Task ShowProcessModal(string message = null)
    {
        this.CurrentProgress.Message = message;
        this.StateHasChanged();

        await this.JS.InvokeVoidAsync("eval", "bootstrap.Modal.getOrCreateInstance(document.getElementById('commonProgressModal')).show()");
        await Task.Delay(1000);
    }

    protected async Task HideProcessModal()
    {
        await this.JS.InvokeVoidAsync("eval", "bootstrap.Modal.getOrCreateInstance(document.getElementById('commonProgressModal')).hide()");
        await Task.Delay(1000);
    }

    protected Task UpdateProgress(string message, double? progress, bool isError, bool isDone)
    {
        this.CurrentProgress.Message = message;
        this.CurrentProgress.Progress = progress;
        this.CurrentProgress.IsError = isError;
        this.CurrentProgress.IsDone = isDone;

        return this.InvokeAsync(this.StateHasChanged);
    }


    public bool DetectPowerpointSummaryFormat(OpenAIChatMessage message)
    {
        if (message.Role == ChatPromptRoles.Assistant &&
            message.IsStreaming == false &&
            (message.Content.Contains("<!--powerpoint-->") ||
             message.Content.Contains("# Slide 1:")))
        {
            return true;
        }
        return false;
    }

    public bool DetectEssay(OpenAIChatMessage message)
    {
        if (message.Role == ChatPromptRoles.Assistant &&
            message.IsStreaming == false &&
            message.Content.Contains("<!--essay-->"))
        {
            return true;
        }
        return false;
    }

    protected void AlertThenForget(string prompt)
    {
        _ = JS.InvokeVoidAsync("alert", prompt);
    }

    public async ValueTask GeneratePowerPoint(OpenAIChatMessage message)
    {
        await this.ShowProcessModal("Preparing your Presentation...");

        var text = message.Content;
        var auth = await this.AuthenticationStateProvider.GetAuthenticationStateAsync();
        var userName = auth.User.Identity.Name;

        var presentation = GenPPTX.ExtractPresentation(text, userName);

        if (presentation.Slides.Count == 0 ||
            presentation.Equals(GenPPTX.EmptyPresentation))
        {
            await this.HideProcessModal();

            SentrySdk.AddBreadcrumb("Chat Message", data: (new { Text = text }).GetDict());
            SentrySdk.CaptureMessage("Cannot Generate PowerPoint");

            this.AlertThenForget("We could not extract presentation from AI Response. Our team was notified and we will take a look into it. Sorry for any inconvinience this may have caused.");

            return;
        }

        var ms = await GenPPTX.Generate($"{GCOpenAIPlatform.Instance.ConfigurationStorage}/PTTGC-Template.pptx", presentation);

        _ = this.SaveFile("presentation.pptx", ms.ToArray());

        await this.HideProcessModal();

    }

    public async ValueTask GenerateDocx(OpenAIChatMessage message)
    {
        await this.ShowProcessModal("Preparing your Word Document...");

        var text = message.RenderedContent ?? message.Content;
        var auth = await this.AuthenticationStateProvider.GetAuthenticationStateAsync();

        var ms = await GenDOCX.Generate(text);

        _ = this.SaveFile("essay.docx", ms.ToArray());

        await this.HideProcessModal();

    }
}
