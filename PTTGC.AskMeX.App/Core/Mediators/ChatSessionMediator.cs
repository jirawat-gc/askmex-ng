using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using PTTGC.AskMeGc;
using PTTGC.AskMeGc.OpenAI;
using PTTGC.AskMeGc.OpenAI.Types;

namespace PTTGC.AskMeX.App.Core.Mediators;

public class ChatSessionMediator
{
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly OpenAIChatSession _session;
    private AccessToken? _accessToken;
    private readonly NavigationManager _navigationManagor;

    public ChatSessionMediator(
        IAccessTokenProvider tokenProvider,
        NavigationManager navigationManagor)
    {
        _tokenProvider = tokenProvider;
        _session = new(GCOpenAIPlatform.Instance, GenAI.MODEL_GPT4OMINI);
        _navigationManagor = navigationManagor;
    }

    public void NotifyUserMessage(string message)
    {
        Task.Run(async () =>
        {
            var hasFetchTokenSuccess = await InitiateRefreshTokenFlow();
            if (hasFetchTokenSuccess == false)
            {
                return;
            }

            _session.AddNewUserPrompt(message);

            var openAIHyperPara = new OpenAIHyperParameter() { Temperature = 0, MaxTokens = 1000 };
            await _session.InvokeAI(openAIHyperPara);
        });
    }

    /// <summary>in case unable to refresh token, app will navgiate to page "/authentication/logout"</summary>
    private async Task<bool> InitiateRefreshTokenFlow()
    {
        var isAccessTokenNull = _accessToken == null;
        // if the remaining time of token is less than 1 minute, then we will proceed as expired token
        var hasAccesssTokenExpired = _accessToken?.Expires > DateTimeOffset.Now.AddMinutes(-1);
        if (isAccessTokenNull || hasAccesssTokenExpired)
        {
            try
            {
                var tokenResult = await _tokenProvider.RequestAccessToken();
                var fecthTokenSucess = tokenResult.TryGetToken(out var token);
                if (fecthTokenSucess == false)
                {
                    throw new Exception("Unable to fetch token");
                }

                if (token == null ||
                    token.Expires < DateTimeOffset.Now.AddMinutes(-1))
                {
                    throw new Exception("Token is invalid or about to expired");
                }

                // assign token to GCOpenAIPlatform only the first time
                if (isAccessTokenNull)
                {
                    GCOpenAIPlatform.Instance.UserToken = token.Value;
                }

                _accessToken = token;
            }
            catch (Exception)
            {
                _navigationManagor.NavigateToLogout("/authentication/logout");
                return false;
            }
        }

        return true;
    }

    public event Action ChatPromptsChanged
    {
        add { _session.ChatPromptsChanged += value; }
        remove { _session.ChatPromptsChanged -= value; }
    }

    public List<OpenAIChatMessage> ChatPrompts => _session.ChatPrompts;

    public event Action<(string token, bool done)> StreamingResponseReceived
    {
        add { _session.StreamingResponseReceived += value; }
        remove { _session.StreamingResponseReceived -= value; }
    }
}
