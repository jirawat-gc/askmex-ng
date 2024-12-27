using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PTTGC.AskMeX.App.Core;
using PTTGC.AskMeX.App.Core.Services;
using System.Text;

namespace PTTGC.AskMeX.App.Pages;

public partial class Welcome : ComponentBase
{

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await Mediator.LoadUserWorkspace();
        ChatBoxStrategy = new TextPromptStrategy(Mediator);
        Mediator.WelcomePage = this;
    }

    public new void StateHasChanged()
    {
        base.StateHasChanged();
    }

    #region Toggleing between Welcome and Chat view

    public MainView MainView { get; set; } = MainView.WelcomeView;

    public DisplayState DisplayState { get; set; } = DisplayState.FullView;

    private string WelcomeViewClasses
    {
        get
        {
            var classes = new StringBuilder();
            classes.Append("welcome-view");
            if (MainView == MainView.WelcomeView)
            {
                classes.Append(" active");
            }
            return classes.ToString();
        }
    }

    private string ChatViewClasses
    {
        get
        {
            var classes = new StringBuilder();
            classes.Append("chat-view");
            if (MainView == MainView.ChatView)
            {
                classes.Append(" active");
            }
            if (DisplayState == DisplayState.LeftHalfView)
            {
                classes.Append(" shring-half");
            }

            return classes.ToString();
        }
    }

    private string BottomContainerClasses
    {
        get
        {
            var classes = new StringBuilder();
            classes.Append("bottom-container");
            if (DisplayState == DisplayState.LeftHalfView)
            {
                classes.Append(" width-half");
            }
            return classes.ToString();
        }
    }

    #endregion

    #region ChatBox related

    private bool preventKeyDownOnChatBox = false;

    private void HandleChatBoxKeyDown(KeyboardEventArgs e)
    {
        // for case when user hold shift key and press enter, meaning user explicitly want to type new line
        // below case is for when user press enter without shift key, which will send message to system (AI)
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            // prevent from sending empty message
            var message = UserMessage;
            Task.Run(() => ChatBoxStrategy!.Submit(message));

            UserMessage = string.Empty;
            preventKeyDownOnChatBox = true;

            // NOTE [YO] : might need to reset chatbox height here otherwise chatbox height might display more than 1 rows for empty string after user hit enter
        }
    }

    // for reset chat box back to idle/original state
    // otherwise user will not be able to type in chat box
    private void HandleChatBoxKeyUp(KeyboardEventArgs e)
    {
        if (preventKeyDownOnChatBox)
        {
            preventKeyDownOnChatBox = false;
        }
    }

    private string UserMessage { get; set; }
#if DEBUG
    // this is test message for development purpose
     = "this is a test, respond with OK";
    //= "Generate 10 sentences of essay";
    //= "Provide me 3 of markdown text with example";
#endif

    public IChatBoxStrategy ChatBoxStrategy { private get; set; }

    #endregion

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

    [Inject]
    public required ChatSessionMediator Mediator { private get; init; }
}

public enum MainView
{
    WelcomeView,
    ChatView
}

public enum DisplayState
{
    FullView,
    LeftHalfView
}