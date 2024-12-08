﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PTTGC.AskMeX.App.Core.Mediators;

namespace PTTGC.AskMeX.App.Pages;

public partial class Welcome : ComponentBase
{

    #region Toggleing between Welcome and Chat view

    const string WelcomeView = "welcomeView";
    const string ChatView = "chatView";
    void OpenChatView()
    {
        CurrentPage = ChatView;
    }

    void ToggleCurrentPage()
    {
        CurrentPage = CurrentPage == WelcomeView ? ChatView : WelcomeView;
    }

    string GetClasses(string viewName, string classesBase)
    {
        if (viewName == CurrentPage)
        {
            return $"{classesBase} active";
        }

        return classesBase;
    }

    string CurrentPage { get; set; } = WelcomeView;

    string WelcomeViewClasses => GetClasses(WelcomeView, "welcome-view");
    string ChatViewClasses => GetClasses(ChatView, "chat-view");

    #endregion

    #region ChatBox related

    bool preventKeyDownOnChatBox = false;

    void HandleChatBoxKeyDown(KeyboardEventArgs e)
    {
        // for case when user hold shift key and press enter, meaning user explicitly want to type new line
        // below case is for when user press enter without shift key, which will send message to system (AI)
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            Mediator.NotifyUserMessage(UserMessage);
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
        // = "this is a test, respond with OK";
        = "Generate 10 sentences of essay";
#endif

    [Inject]
    public required ChatSessionMediator Mediator { private get; init; }

    #endregion
}
