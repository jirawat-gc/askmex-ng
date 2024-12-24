using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using PTTGC.AskMeGc;
using PTTGC.AskMeGc.DocumentEmbedding;
using PTTGC.AskMeGc.OpenAI;
using PTTGC.AskMeGc.OpenAI.Types;
using PTTGC.AskMeGc.Workspace;
using PTTGC.AskMeX.App.Components;
using PTTGC.AskMeX.App.Core.Types;
using PTTGC.AskMeX.App.Pages;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UglyToad.PdfPig;

namespace PTTGC.AskMeX.App.Core.Services;

public class ChatSessionMediator
{
    private const string APP_ID = "askmexspaces";
    private readonly string SESSION_ID = Guid.NewGuid().ToString();
    private const string HASH_FIELD = "hash";

    private readonly IAccessTokenProvider _tokenProvider;
    private readonly OpenAIChatSession _session;
    private AccessToken? _accessToken;
    private readonly NavigationManager _navigationManagor;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly GCOpenAIPlatform _gcOpenAIPlatform = GCOpenAIPlatform.Instance;
    private readonly AskMeXGateKeeperClient _askMeXGateKeeperClient = AskMeXGateKeeperClient.Instance;
    private readonly IJSRuntime _jSRuntime;
    private IJSObjectReference _pdfScript;
    private ContainerInfo? _userContainerInfo;
    private List<WorkspaceFile>? _workspaceFiles;

    public ChatSessionMediator(
        IAccessTokenProvider tokenProvider,
        NavigationManager navigationManagor,
        AuthenticationStateProvider authenticationStateProvider,
        IJSRuntime jSRuntime)
    {
        _tokenProvider = tokenProvider;
        _session = new(_gcOpenAIPlatform, GenAI.MODEL_GPT4OMINI);
        _navigationManagor = navigationManagor;
        _authenticationStateProvider = authenticationStateProvider;
        _jSRuntime = jSRuntime;
    }

    #region Methods

    public async Task UserSendMessage(string message)
    {
        await InitiateRefreshTokenFlow(async () =>
        {
            _session.AddNewUserPrompt(message);

            var openAIHyperPara = new OpenAIHyperParameter() { Temperature = 0, MaxTokens = 1000 };
            await _session.InvokeAI(openAIHyperPara);
        });
    }

    public async Task UploadPdfFile(IBrowserFile file)
    {
    }

    private string ComputeSHA1Hash(byte[] data)
    {
        using var sha1 = SHA1.Create();
        byte[] hashBytes = sha1.ComputeHash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Load user workspace (files) which needed to call the first method of this class
    /// </summary>
    /// <returns></returns>
    public async Task LoadUserWorkspace()
    {
        await InitiateRefreshTokenFlow(async () =>
        {
            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = state.User;
            var fullWorkspaceName = _askMeXGateKeeperClient.GetWorkspaceName(APP_ID, user);
            var workspaces = await _askMeXGateKeeperClient.ListWorkspaces(APP_ID, SESSION_ID);
            var userWorkSpace = workspaces.FirstOrDefault(x => x.name == fullWorkspaceName);
            if (userWorkSpace == null)
            {
                var userObjectId = _askMeXGateKeeperClient.GetUserObjectId(user);
                _userContainerInfo = await _askMeXGateKeeperClient.CreateWorkspace(APP_ID, SESSION_ID, userObjectId);
            }
            else
            {
                _userContainerInfo = await _askMeXGateKeeperClient.AccessWorkspace(APP_ID, SESSION_ID, fullWorkspaceName);
            }

            // force welcome page to re-render workspace files
            WelcomePage.StateHasChanged();
        });
    }

    /// <summary>in case unable to refresh token, app will navgiate to page "/authentication/logout"</summary>
    private async Task InitiateRefreshTokenFlow(Action callBack)
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
                    _askMeXGateKeeperClient.AccessToken = token.Value;
                    _gcOpenAIPlatform.UserToken = token.Value;
                }

                _accessToken = token;
            }
            catch (Exception)
            {
                _navigationManagor.NavigateToLogout("/authentication/logout");
                return;
            }
        }

        callBack.Invoke();
    }

    private WorkspaceFile GetWorkspaceFile(string file)
    {
        var thumbnailBlobName = _userContainerInfo!.GetThubmnailBlobName(file);
        var thumbnailClient = _userContainerInfo!
            .GetContainerClient()
            .GetBlobClient(thumbnailBlobName);
        var fileName = Path.GetFileName(file);
        return new WorkspaceFile()
        {
            Name = fileName,
            ThumbnailUrl = thumbnailClient.Uri.ToString(),
            FileExtension = Path.GetExtension(file)
        };
    }

    #endregion

    #region Mediator Pattern

    public async Task OnConfirmToUseSelectedWorkspaceFileToSummarize(WorkspaceFile file)
    {
        WelcomePage.HideWorkspaceFileBrowserView();
        WelcomePage.StateHasChanged();

        // TODO: load json not pdf file put into tag/DOM and insert as prompt
        // document inteligence will be able to read the content
    }

    public async Task OnSelectNewLocalPdfFileToSummarize(InputFileChangeEventArgs e)
    {
        await WelcomePage.HideFileOptionsModal();
        WelcomePage.OpenChatView();
        WelcomePage.StateHasChanged();
        var file = e.File;
        var fileName = file.Name;

        // NOTE : might need to check for _userContainerInfo is null or check for expired token here
        var fileContentType = file.ContentType;
        if (fileContentType != "application/pdf")
        {
            throw new Exception("Invalid file type, only PDF file is allowed");
        }

        await Task.Run(async () =>
        {
            // upload pdf, thumpnail, json content, etc to blob storage
            using var stream = file.OpenReadStream();
            using var pdfStream = new MemoryStream();
            await stream.CopyToAsync(pdfStream);
            stream.Close();
            byte[] fileBytes = pdfStream.ToArray();
            var fileHash = ComputeSHA1Hash(fileBytes);

            // Get BlobClient
            var client = _userContainerInfo!.GetContainerClient();
            var pdfBlobName = _userContainerInfo.GetDocumentBlobName(fileName);
            var pdfBlobClient = client.GetBlobClient(pdfBlobName);

            Func<Task<byte[]>> generateThumpnail = async () =>
            {
                var pdfUrl = pdfBlobClient.Uri;

                string script = "/js/pdfscript.js";
                if (_pdfScript == null)
                {
                    _pdfScript = await _jSRuntime.InvokeAsync<IJSObjectReference>("import", script);
                }
                var pdfProcessor = await _pdfScript.InvokeAsync<IJSObjectReference>("pdfHandling");
                var previewDataUri = await pdfProcessor.InvokeAsync<string>("generateThumbnail", pdfUrl);

                int base64Index = previewDataUri.IndexOf(";base64,") + ";base64,".Length;

                byte[] cover = new byte[0];
                try
                {
                    cover = Convert.FromBase64String(previewDataUri.Substring(base64Index));
                }
                catch (Exception)
                {
                    var data = new Dictionary<string, string>()
                    {
                        { "base64Index", base64Index.ToString() },
                        { "dataUri", previewDataUri.Substring(0, 50)}
                    };
                    SentrySdk.AddBreadcrumb("Cover data", data: data);

                    SentrySdk.CaptureMessage("Could not generate cover");
                }

                return cover;
            };

            // Upload File Method
            Func<Task<EmbeddedDocument>> uploadPdfFile = async () =>
            {
                // uplaod pdf file and embed content (images and table)
                pdfStream.Seek(0, System.IO.SeekOrigin.Begin);
                var document = await DocEmbed.UploadAndEmbedPDFFile(
                    _userContainerInfo!,
                    pdfStream,
                    fileName,
                    APP_ID,
                    SESSION_ID,
                    (message, progress, isError, isDone) =>
                    {
                        if (isError)
                        {
                            throw new Exception(message);
                        }
                        return Task.CompletedTask;
                    });

                // Set Metadata
                await pdfBlobClient.SetMetadataAsync(
                    new Dictionary<string, string> { { HASH_FIELD, fileHash } });

                // generate thumpnail and upload
                var thumbnail = await generateThumpnail();
                await DocEmbed.UploadThumbnail(_userContainerInfo, fileName, thumbnail);

                return document;
            };

            EmbeddedDocument document;
            var exists = await pdfBlobClient.ExistsAsync();
            if (exists.Value)
            {
                // Retrieve metadata
                var properties = await pdfBlobClient.GetPropertiesAsync();
                var metadata = properties.Value.Metadata;
                // NOTE : has not handle in case there is no hash field in Metadata
                var blobHash = metadata[HASH_FIELD];
                if (fileHash == blobHash)
                {
                    // Same file has been uploaded, tread as uploaded file success
                    // skip to read text from PDF file
                    var jsonBlobName = _userContainerInfo.GetDocumentJsonBlobName(fileName);
                    var jsonBlobClient = _userContainerInfo.GetContainerClient().GetBlobClient(jsonBlobName);
                    document = await jsonBlobClient.GetJson<EmbeddedDocument>();
                }
                else
                {
                    // TODO: asking user whether to overwrite the file
                    throw new NotImplementedException();
                }
            }
            else
            {
                document = await uploadPdfFile();

                // add new file to workspace, this approach will only work if there is only one active user per account
                _workspaceFiles!.Add(GetWorkspaceFile(fileName));
                WelcomePage.StateHasChanged();
            }

            // read text from PDF file
            var pdfContentBuilder = new StringBuilder();
            pdfContentBuilder.Append("<content>");
            foreach (var pageContent in document.PageContents)
            {
                pdfContentBuilder.Append("<page>");
                pdfContentBuilder.Append(pageContent);
                pdfContentBuilder.Append("</page>");
            }
            pdfContentBuilder.Append("</content>");

            // insert content (context) in file to ChatPrompts's system
            _session.ChatPrompts.Add(new()
            {
                Role = ChatPromptRoles.System,
                Content = pdfContentBuilder.ToString()
            });

            // insert user prompt (command)
            var attachedUri = pdfBlobClient.Uri.AbsoluteUri;
            _session.AddNewUserPrompt(
                "Base on <content> in latest system's prompt, Summarize content",
                attachedDocumentUri: attachedUri);

            // summit prompts
            await _session.InvokeAI(new() { Temperature = 0, MaxTokens = 1000 });
        });
    }

    public Welcome WelcomePage { private get; set; }

    public ChatSession ChatSessionComponent { private get; set; }

    #endregion

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

    public IEnumerable<WorkspaceFile> WorksapceFiles
    {
        get
        {
            if (_userContainerInfo == null)
            {
                return Array.Empty<WorkspaceFile>();
            }

            if (_workspaceFiles == null)
            {
                var files = _userContainerInfo.Files;
                var supportedFileTypes = new HashSet<string>() { ".pdf", ".xlsx" };
                Func<string, bool> isSupportedFileType = file =>
                {
                    var extension = Path.GetExtension(file);
                    return supportedFileTypes.Contains(extension);
                };
                _workspaceFiles = files
                    .Where(file => isSupportedFileType(file))
                    .Select(GetWorkspaceFile).ToList();
            }

            return _workspaceFiles;
        }
    }
}
