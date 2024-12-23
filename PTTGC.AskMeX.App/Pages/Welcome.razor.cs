using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PTTGC.AskMeX.App.Core.Services;
using System.Text;

namespace PTTGC.AskMeX.App.Pages;

public partial class Welcome : ComponentBase
{

    #region Toggleing between Welcome and Chat view

    const string WelcomeView = "welcomeView";
    const string ChatView = "chatView";
    bool isWorkspaceFileBrowserActive = false;
    public void OpenChatView()
    {
        CurrentPrimaryView = ChatView;
    }

    void ToggleCurrentPage()
    {
        CurrentPrimaryView = CurrentPrimaryView == WelcomeView ? ChatView : WelcomeView;
    }

    void ToggleWorkspaceFileBrowserView()
    {
        isWorkspaceFileBrowserActive = !isWorkspaceFileBrowserActive;
    }

    public void OpenWorkspaceFileBrowserView()
    {
        isWorkspaceFileBrowserActive = true;
    }

    public new void StateHasChanged()
    {
        base.StateHasChanged();
    }

    string currentPrimaryView = WelcomeView;
    string CurrentPrimaryView
    {
        get => currentPrimaryView;
        set
        {
            currentPrimaryView = value;
            if (value != ChatView)
            {
                isWorkspaceFileBrowserActive = false;
            }
        }
    }

    string WelcomeViewClasses
    {
        get
        {
            var classes = new StringBuilder();
            classes.Append("welcome-view");
            if (CurrentPrimaryView == WelcomeView)
            {
                classes.Append(" active");
            }
            return classes.ToString();
        }
    }

    string ChatViewClasses
    {
        get
        {
            var classes = new StringBuilder();
            classes.Append("chat-view");
            if (CurrentPrimaryView == ChatView)
            {
                classes.Append(" active");
            }
            if (isWorkspaceFileBrowserActive)
            {
                classes.Append(" shring-half");
            }

            return classes.ToString();
        }
    }

    string OpenWorkspaceFileBrowserButtonClasses
    {
        get
        {
            var classes = new StringBuilder();
            classes.Append("open-workspace-file-broswer-btn");
            if (CurrentPrimaryView == ChatView)
            {
                classes.Append(" active");
            }
            if (isWorkspaceFileBrowserActive)
            {
                classes.Append(" workspace-file-browser-active");
            }
            return classes.ToString();
        }
    }

    string WrokspaceFileBrowserViewClasses
    {
        get
        {
            var classes = new StringBuilder();
            classes.Append("workspace-file-browser");
            if (isWorkspaceFileBrowserActive)
            {
                classes.Append(" active");
            }
            return classes.ToString();
        }
    }

    string BottomContainerClasses
    {
        get
        {
            var classes = new StringBuilder();
            classes.Append("bottom-container");
            if (isWorkspaceFileBrowserActive)
            {
                classes.Append(" width-half");
            }
            return classes.ToString();
        }
    }

    #endregion

    #region ChatBox related

    bool preventKeyDownOnChatBox = false;

    void HandleChatBoxKeyDown(KeyboardEventArgs e)
    {
        // for case when user hold shift key and press enter, meaning user explicitly want to type new line
        // below case is for when user press enter without shift key, which will send message to system (AI)
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            var message = UserMessage;
            Task.Run(() => Mediator.UserSendMessage(message));
            UserMessage = string.Empty;
            preventKeyDownOnChatBox = true;

            // NOTE [YO] : might need to reset chatbox height here otherwise chatbox height might display more than 1 rows for empty string after user hit enter
        }
    }

    // for reset chat box back to idle/original state
    // otherwise user will not be able to type in chat box
    void HandleChatBoxKeyUp(KeyboardEventArgs e)
    {
        if (preventKeyDownOnChatBox)
        {
            preventKeyDownOnChatBox = false;
        }
    }

    string UserMessage { get; set; }
#if DEBUG
    // this is test message for development purpose
     = "this is a test, respond with OK";
    //= "Generate 10 sentences of essay";
    //= "Provide me 3 of markdown text with example";
#endif

    [Inject]
    public required ChatSessionMediator Mediator { private get; init; }

    #endregion

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await Mediator.LoadUserWorkspace();
        Mediator.WelcomePage = this;
    }

    #region File uploading

    private Modal fileOptionsModal;

    private void SelectFileToWorkSpace(InputFileChangeEventArgs e)
    {
        Task.Run(() => Mediator.UploadPdfFile(e.File));
    }

    private async Task ShowFileOptionsModal()
    {
        await fileOptionsModal!.ShowAsync();
    }

    public async Task HideFileOptionsModal()
    {
        await fileOptionsModal!.HideAsync();
    }

    public async Task ChooseLocalFileToSummarize()
    {
        await JS.InvokeVoidAsync("clickById", "upload-file-to-summarize-input");
    }

    [Inject]
    public required IJSRuntime JS { private get; init; }

    #endregion
}
