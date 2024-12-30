using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using PTTGC.AskMeGc;
using PTTGC.AskMeGc.Brownie;
using PTTGC.AskMeGc.DocumentEmbedding;
using PTTGC.AskMeGc.OpenAI;
using PTTGC.AskMeGc.OpenAI.Types;
using PTTGC.AskMeGc.Workspace;
using PTTGC.AskMeX.App.Components;
using PTTGC.AskMeX.App.Core.Configurations;
using PTTGC.AskMeX.App.Core.Types;
using PTTGC.AskMeX.App.Pages;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PTTGC.AskMeX.App.Core.Services;

public class ChatSessionMediator
{
    private const string APP_ID = "askmexspaces";
    private readonly string SESSION_ID = Guid.NewGuid().ToString();
    private const string HASH_FIELD = "hash";

    private readonly IAccessTokenProvider _tokenProvider;
    private readonly OpenAIChatSession _session;
    private readonly NavigationManager _navigationManagor;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    #region Might need to separate this into another class to control the flow of how to access token
    /// <summary>
    /// Call method "InitiateRefreshTokenFlow" before using this instance
    /// </summary>
    private readonly GCOpenAIPlatform _gcOpenAIPlatform = GCOpenAIPlatform.Instance;
    /// <summary>
    /// Call method "InitiateRefreshTokenFlow" before using this client
    /// </summary>
    private readonly AskMeXGateKeeperClient _askMeXGateKeeperClient = AskMeXGateKeeperClient.Instance;
    /// <summary>
    /// Call method "InitiateRefreshTokenFlow" before using this class
    /// </summary>
    private ContainerInfo? _userContainerInfo;
    private AccessToken? _accessToken;
    #endregion

    private string? _fullWorkspaceName;
    private readonly IJSRuntime _jSRuntime;
    private IJSObjectReference? _pdfScript;
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

        #region DEBUG settings
#if DEBUG
        if (DebugSettings.IsTestingFilesSearchingState)
        {
#pragma warning disable CS0162 // Unreachable code detected
            FilesBeingSearched = new();
#pragma warning restore CS0162 // Unreachable code detected
            for (int i = 0; i < 3; i++)
            {
                FilesBeingSearched.Add(new()
                {
                    BlobName = "abc",
                    FileExtension = ".pdf",
                    Name = "Getting started with OneDrive 2.pdf",
                    ThumbnailUrl = "https://gcazdtestbenchst01.blob.core.windows.net/workspace-askmexspaces-ce1ef78f-99f8-4277-8eb3-48afa19ecf82/fileinventory/thumbnail/Getting started with OneDrive 2.jpg?sv=2024-11-04&st=2024-12-30T09%3A07%3A45Z&se=2024-12-31T09%3A07%3A45Z&sr=c&sp=racwdxyltmei&sig=0OTkexD2mjK2w69k%2B6SxKqv3f1RkG4Zq3%2B3pRtII8bU%3D"
                });
            }
        }
        if (DebugSettings.IsTestingPostProcessingAssistantPrompWithReferences)
        {
            var message = new OpenAIChatMessage()
            {
                Role = ChatPromptRoles.Assistant,
                Content = "Microsoft OneDrive is cloud storage that you can access from anywhere. It helps you stay organized, access your important documents, photos, and other files from any device[REF-0], and share those files with friends, family, or coworkers. [REF-1]",
                References = new()
                {
                    new()
                    {
                        EmbededDocumentDto = new()
                        {
                            OriginalFileName = "Motor_LMG_ชั้น2บวก_No.pdf",
                            Description ="Insurance Company: LMG, Insurance Type: Automobile, Class: 2 Plus (ชั้น 2 บวก, ชั้น 2+, Class 2+)",
                            Url ="https://gcazdtestbenchst01.blob.core.windows.net/workspace-brownie-postmantest/fileinventory/files/Motor_LMG_ชั้น2บวก_No.pdf?sv=2024-11-04&st=2024-12-27T08%3A23%3A05Z&se=2024-12-27T12%3A23%3A05Z&sr=c&sp=rl&sig=LjL0dMx8PntOovDwtfxUke%2FcGNcpdvb%2Fn29EMJcIl6c%3D",
                            JsonUrl ="https://gcazdtestbenchst01.blob.core.windows.net/workspace-brownie-postmantest/fileinventory/Motor_LMG_ชั้น2บวก_No.json?sv=2024-11-04&st=2024-12-27T08%3A23%3A05Z&se=2024-12-27T12%3A23%3A05Z&sr=c&sp=rl&sig=LjL0dMx8PntOovDwtfxUke%2FcGNcpdvb%2Fn29EMJcIl6c%3D",
                            CoverUrl ="https://gcazdtestbenchst01.blob.core.windows.net/workspace-brownie-postmantest/fileinventory/thumbnail/Motor_LMG_ชั้น2บวก_No.jpg?sv=2024-11-04&st=2024-12-27T08%3A23%3A05Z&se=2024-12-27T12%3A23%3A05Z&sr=c&sp=rl&sig=LjL0dMx8PntOovDwtfxUke%2FcGNcpdvb%2Fn29EMJcIl6c%3D",
                            LastModified = new DateTime(2024, 10, 18, 12,56,20, DateTimeKind.Utc) ,//"2024-10-18T19:56:20.936+07:00",
                            TotalPages =13
                        },
                        SourceId ="Motor_LMG_ชั้น2บวก_No.pdf-13-13",
                        InternalId ="REF-0",
                        SourceEmbeddingChunkContentKey ="Motor_LMG_ชั้น2บวก_No.pdf",
                        SourcePage =13,
                        SourcePageEnd =13,
                        Knowledge ="ข้อ 4. การสละสิทธิ\nในกรณีที่มีความเสียหายหรือสูญหายต่อรถยนต์ เมื่อบุคคลอื่นเป็นผู้ใช้รถยนต์โดยได้รับความ ยินยอมจากผู้เอาประกันภัย บริษัทสละสิทธิในการไล่เบี้ยจากผู้ใช้รถยนต์นั้น เว้นแต่การใช้โดยบุคคลของ สถานให้บริการเกี่ยวกับการซ่อมแซมรถ การทำความสะอาดรถ การบำรุงรักษารถ หรือการติดตั้งอุปกรณ์ เพิ่มเติม เมื่อรถยนต์ได้ส่งมอบให้เพื่อรับบริการนั้น\nข้อ 5. การยกเว้นรถยนต์สูญหาย ไฟไหม้ การประกันภัยนี้ไม่คุ้มครองความสูญหาย หรือไฟไหม้อัน เกิดจาก\n5.1 ความเสียหายหรือสูญหายอันเกิดจากการลักทรัพย์ หรือยักยอกทรัพย์ โดยคู่สมรส หรือบุคคลซึ่งอยู่กินกันฉันสามีภรรยาโดยไม่ได้จดทะเบียนสมรส หรือบุคคลที่เป็นหุ้นส่วนทางธุรกิจ กับผู้เอาประกันภัย หรือผู้ค้ำประกันตามสัญญาเช่าซื้อรถยนต์ หรือบุคคลได้รับมอบรถยนต์หรือ ครอบครองรถยนต์ตามสัญญายืม สัญญาเช่า สัญญาเช่าซื้อ หรือสัญญาจำนำ หรือบุคคลที่จะกระทำ สัญญาดังกล่าวข้างต้น ทั้งนี้ไม่ว่าบุคคลดังกล่าวมีเจตนาแท้จริงจะทำสัญญาดังกล่าวหรือไม่ก็ตาม\n5.2 การใช้รถยนต์นอกอาณาเขตที่คุ้มครอง",
                        KnowledgeSummary ="- Waiver of rights in case of damage or loss when another person uses the vehicle with the insured's consent.\n- Exclusions related to theft or loss caused by certain individuals (e.g., spouse, business partners).\n- Exclusions for vehicle use outside the covered territory.",
                        Certainty =0.8581109057509099,
                        Usefulness =2,
                        Rationale ="The content discusses exclusions related to theft or loss, which is relevant to the user's inquiry about coverage for vehicle theft under the LMG 2 plus insurance.",
                        RelatedKnowledge =[]
                    },
                    new()
                    {
                        EmbededDocumentDto = new()
                        {
                            OriginalFileName = "Motor_LMG_ชั้น3บวก_No.pdf",
                            Description ="Insurance Company: LMG, Insurance Type: Automobile, Class: 2 Plus (ชั้น 2 บวก, ชั้น 2+, Class 2+)",
                            Url ="https://gcazdtestbenchst01.blob.core.windows.net/workspace-brownie-postmantest/fileinventory/files/Motor_LMG_ชั้น2บวก_No.pdf?sv=2024-11-04&st=2024-12-27T08%3A23%3A05Z&se=2024-12-27T12%3A23%3A05Z&sr=c&sp=rl&sig=LjL0dMx8PntOovDwtfxUke%2FcGNcpdvb%2Fn29EMJcIl6c%3D",
                            JsonUrl ="https://gcazdtestbenchst01.blob.core.windows.net/workspace-brownie-postmantest/fileinventory/Motor_LMG_ชั้น2บวก_No.json?sv=2024-11-04&st=2024-12-27T08%3A23%3A05Z&se=2024-12-27T12%3A23%3A05Z&sr=c&sp=rl&sig=LjL0dMx8PntOovDwtfxUke%2FcGNcpdvb%2Fn29EMJcIl6c%3D",
                            CoverUrl ="https://gcazdtestbenchst01.blob.core.windows.net/workspace-brownie-postmantest/fileinventory/thumbnail/Motor_LMG_ชั้น2บวก_No.jpg?sv=2024-11-04&st=2024-12-27T08%3A23%3A05Z&se=2024-12-27T12%3A23%3A05Z&sr=c&sp=rl&sig=LjL0dMx8PntOovDwtfxUke%2FcGNcpdvb%2Fn29EMJcIl6c%3D",
                            LastModified = new DateTime(2024, 10, 18, 12,56,20, DateTimeKind.Utc) ,//"2024-10-18T19:56:20.936+07:00",
                            TotalPages =13
                        },
                        SourceId ="Motor_LMG_ชั้น3บวก_No.pdf-2-3",
                        InternalId ="REF-1",
                        SourceEmbeddingChunkContentKey ="Motor_LMG_ชั้น3บวก_No.pdf",
                        SourcePage =2,
                        SourcePageEnd =3,
                        Knowledge ="ข้อ 4. การสละสิทธิ\nในกรณีที่มีความเสียหายหรือสูญหายต่อรถยนต์ เมื่อบุคคลอื่นเป็นผู้ใช้รถยนต์โดยได้รับความ ยินยอมจากผู้เอาประกันภัย บริษัทสละสิทธิในการไล่เบี้ยจากผู้ใช้รถยนต์นั้น เว้นแต่การใช้โดยบุคคลของ สถานให้บริการเกี่ยวกับการซ่อมแซมรถ การทำความสะอาดรถ การบำรุงรักษารถ หรือการติดตั้งอุปกรณ์ เพิ่มเติม เมื่อรถยนต์ได้ส่งมอบให้เพื่อรับบริการนั้น\nข้อ 5. การยกเว้นรถยนต์สูญหาย ไฟไหม้ การประกันภัยนี้ไม่คุ้มครองความสูญหาย หรือไฟไหม้อัน เกิดจาก\n5.1 ความเสียหายหรือสูญหายอันเกิดจากการลักทรัพย์ หรือยักยอกทรัพย์ โดยคู่สมรส หรือบุคคลซึ่งอยู่กินกันฉันสามีภรรยาโดยไม่ได้จดทะเบียนสมรส หรือบุคคลที่เป็นหุ้นส่วนทางธุรกิจ กับผู้เอาประกันภัย หรือผู้ค้ำประกันตามสัญญาเช่าซื้อรถยนต์ หรือบุคคลได้รับมอบรถยนต์หรือ ครอบครองรถยนต์ตามสัญญายืม สัญญาเช่า สัญญาเช่าซื้อ หรือสัญญาจำนำ หรือบุคคลที่จะกระทำ สัญญาดังกล่าวข้างต้น ทั้งนี้ไม่ว่าบุคคลดังกล่าวมีเจตนาแท้จริงจะทำสัญญาดังกล่าวหรือไม่ก็ตาม\n5.2 การใช้รถยนต์นอกอาณาเขตที่คุ้มครอง",
                        KnowledgeSummary ="- Waiver of rights in case of damage or loss when another person uses the vehicle with the insured's consent.\n- Exclusions related to theft or loss caused by certain individuals (e.g., spouse, business partners).\n- Exclusions for vehicle use outside the covered territory.",
                        Certainty =0.8581109057509099,
                        Usefulness =2,
                        Rationale ="The content discusses exclusions related to theft or loss, which is relevant to the user's inquiry about coverage for vehicle theft under the LMG 2 plus insurance.",
                        RelatedKnowledge =[]
                    }
                }
            };
            message.Content = PostProcessReferences(message.Content);
            _session.ChatPrompts.Add(message);
        }
#endif
        #endregion
    }

    #region Methods

    public async Task SendUserMessage(string message)
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
        throw new NotImplementedException();
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
            // TODO: move this code to common library
            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = state.User;
            _fullWorkspaceName = _askMeXGateKeeperClient.GetWorkspaceName(APP_ID, user);
            var workspaces = await _askMeXGateKeeperClient.ListWorkspaces(APP_ID, SESSION_ID);
            var userWorkSpace = workspaces.FirstOrDefault(x => x.name == _fullWorkspaceName);
            if (userWorkSpace == null)
            {
                var userObjectId = _askMeXGateKeeperClient.GetUserObjectId(user);
                _userContainerInfo = await _askMeXGateKeeperClient.CreateWorkspace(APP_ID, SESSION_ID, userObjectId);
            }
            else
            {
                _userContainerInfo = await _askMeXGateKeeperClient.AccessWorkspace(APP_ID, SESSION_ID, _fullWorkspaceName);
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

    private WorkspaceFile GetWorkspaceFile(string blobName)
    {
        var thumbnailBlobName = _userContainerInfo!.GetThubmnailBlobName(blobName);
        var thumbnailClient = _userContainerInfo!
            .GetContainerClient()
            .GetBlobClient(thumbnailBlobName);
        var fileName = Path.GetFileName(blobName);
        return new WorkspaceFile()
        {
            Name = fileName,
            ThumbnailUrl = thumbnailClient.Uri.ToString(),
            FileExtension = Path.GetExtension(blobName),
            BlobName = blobName,
        };
    }

    #endregion

    #region Mediator Pattern

    #region Upload/Summarize PDF File

    public async Task OnConfirmToUseSelectedWorkspaceFileToSummarize(WorkspaceFile file)
    {
        OnHideWorkspaceFileBrowserView();

        if (file.FileExtension != ".pdf")
        {
            throw new NotSupportedException("this feature only support .pdf file type");
        }

        var fileName = file.Name;
        var client = _userContainerInfo!.GetContainerClient();
        var jsonBlobName = _userContainerInfo.GetDocumentJsonBlobName(fileName);
        var jsonBlobClient = client.GetBlobClient(jsonBlobName);
        var document = await jsonBlobClient.GetJson<EmbeddedDocument>();

        var pdfBlobName = _userContainerInfo.GetDocumentBlobName(fileName);
        var pdfBlobClient = client.GetBlobClient(pdfBlobName);
        var pdfUrl = pdfBlobClient.Uri.ToString();
        await SummarizeFileContent(document, pdfUrl);
    }

    public async Task OnSelectNewLocalPdfFileToSummarize(InputFileChangeEventArgs e)
    {
        await WelcomePage.HideFileOptionsModal();
        OnSwitchMainViewToChatView();
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
            Func<Task<EmbeddedDocument>> uploadPdfAndThumbnail = async () =>
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
                        // update progress on UI  here
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
                document = await uploadPdfAndThumbnail();

                // add new file to workspace, this approach will only work if there is only one active user per account
                _workspaceFiles!.Add(GetWorkspaceFile(fileName));
                WelcomePage.StateHasChanged();
            }

            var attachedUri = pdfBlobClient.Uri.AbsoluteUri;
            await SummarizeFileContent(document, attachedUri);
        });
    }

    /// <summary>
    /// Read content from document and insert as ChatPrompts's system and inser user prompt to summarize content
    /// </summary>
    private async Task SummarizeFileContent(EmbeddedDocument document, string fileUrl)
    {
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
        var systemPrompt = new OpenAIChatMessage()
        {
            Role = ChatPromptRoles.System,
            Content = pdfContentBuilder.ToString(),
            IsExcludedFromContext = false
        };
        _session.ChatPrompts.Add(systemPrompt);

        // insert user prompt (command)
        _session.AddNewUserPrompt(
            "Base on <content> in latest system's prompt, Summarize content",
            attachedDocumentUri: fileUrl);

        // summit prompts
        await _session.InvokeAI(new() { Temperature = 0, MaxTokens = 1000 });

        // exclude system prompt from context to unnecessary token consumtion
        systemPrompt.IsExcludedFromContext = true;
    }

    public async Task OnChoosingExistingFileToSummarize()
    {
        await WelcomePage.HideFileOptionsModal();
        OnSwitchMainViewToChatView();
        OnOpenWorkspaceFileBrowserView(FileBrowserMode.OnePdfToSummarize);
    }

    #endregion

    #region Search On Multiple Documents

    private HashSet<WorkspaceFile>? _workspaceFilesForSearch;
    public HashSet<WorkspaceFile>? FilesBeingSearched { get; private set; }

    public Task OnConfirmToUseSelectedWorkspaceFilesToSearch(IEnumerable<WorkspaceFile> files)
    {
        _workspaceFilesForSearch = new(files, new WorkspaceFileBlobNameComparer());
        OnHideWorkspaceFileBrowserView();

        // generate system prompt for telling user to input search query
        var assistantPromptBuilder = new StringBuilder();
        assistantPromptBuilder.AppendLine("For the Following files you have selected :");
        assistantPromptBuilder.AppendLine();
        var fileNumber = 0;
        foreach (var file in _workspaceFilesForSearch)
        {
            fileNumber++;
            assistantPromptBuilder.AppendLine($"{fileNumber}. {file.Name}");
        }
        assistantPromptBuilder.AppendLine();
        assistantPromptBuilder.AppendLine("**Please specifiy the question which you want to search in selected files above** or type `Cancel` if you wish to cancel the search.");
        _session.ChatPrompts.Add(new()
        {
            Role = ChatPromptRoles.Assistant,
            Content = assistantPromptBuilder.ToString()
        });

        WelcomePage.ChatBoxStrategy = new SearchFromWorkspacePDFsStrategy(this);
        ChatSessionComponent.StateHasChanged();
        return Task.CompletedTask;
    }

    public void OnCancelSearchFromWorkspacePDFs()
    {
        _session.AddNewUserPrompt("Cancel");

        WelcomePage.ChatBoxStrategy = new TextPromptStrategy(this);
        _workspaceFilesForSearch = null;
        _session.ChatPrompts.Add(new()
        {
            Role = ChatPromptRoles.Assistant,
            Content = "Search operation has been cancel."
        });
        ChatSessionComponent.StateHasChanged();
    }

    public async Task OnSubmitSearchQueryForWorkspacePDFs(string query)
    {
        await InitiateRefreshTokenFlow(async () =>
        {
            _session.AddNewUserPrompt(query);

            var brownieBotInfo = new BrownieBotInfo()
            {
                BotId = _fullWorkspaceName,
                Settings = new()
                {
                    { "HyperParameter_MaxTokens", "1000" },
                    { "HyperParameter_Temperature", "0.25" },
                }
            };
            var bot = new BrownieBotInstance();
            bot.BotWorkspace = _userContainerInfo!;
            bot.BrownieBotInfo = brownieBotInfo;

            bot.BotSettings = brownieBotInfo.GetSettings();
            bot.BotSettings.EnsuresSettingsValid();

            bot.OpenAIHyperParameter.MaxTokens = bot.BotSettings.HyperParameter_MaxTokens;
            bot.OpenAIHyperParameter.Temperature = bot.BotSettings.HyperParameter_Temperature;
            bot.SelectedPersona = bot.BotSettings.Prompt_PersonaList[0];

            // load json documents
            var blobNames = _workspaceFilesForSearch!.Select(file => file.BlobName);
            await bot.ReadBotDocument(_userContainerInfo!, blobNames);

            bot.IsSearchOnly = false;

            bot.TokenIsRequired = () =>
            {
                return Task.FromResult(_accessToken!.Value);
            };

            // normal mode (not priority)
            // TODO: wiring send message btn to action

            // search mode (not priority)
            // TODO: change UI when in search mode without file enter and nothing happen
            // TODO: change UI when user have selected files, enable send button
            // TODO: change UI to display selected files

            // ---

            // TODO: convert [REF1] to markdown reference

            var assistantReplyStatus = new OpenAIChatMessage()
            {
                Role = CustomRole.AssistantReplyStatus,
                Content = "กำลังเตรียมข้อมูล",
                IsStreaming = false,
                CustomType = AssistantReplyCustomerType.AnswerProgressStatus,
                IsExcludedFromContext = true,
            };

            var messageBuilder = new StringBuilder();
            var message = new OpenAIChatMessage()
            {
                Role = ChatPromptRoles.Assistant,
                Content = string.Empty,
                IsStreaming = true,
                References = new()
            };

            FilesBeingSearched = new(new WorkspaceFileBlobNameComparer());
            var fileByFileName = _workspaceFilesForSearch!.ToDictionary(file => file.Name);
            ChatSessionComponent.FilesSearchingState = FilesSearchingState.PendingAI;
            this.ChatPrompts.Add(assistantReplyStatus);

            // as state would be yeilding status -> knowledge -> status -> token in order
            // expected state flow would be
            // FileSearchingState : PendingAI -> DisplayingLatestStatus -> SearchingFiles -> ReplyingToUser
            // the reason we should not embed FileSearchingState into the OpenAIChatMessage is because
            // in order to display message as requirement, we need 2 messages (assisnant reply status and assisnant reply message)
            // and need to display cover image with magnifying glass icon which does not include in the OpenAIChatMessage
            // that is why FileSearchingState should not be state of chat message but instead state of operation (search operation)

            Func<string, Task> updateStatusText = (status) =>
            {
                if (string.IsNullOrEmpty(status))
                {
                    return Task.CompletedTask;
                }

                var isPendingAIState = ChatSessionComponent.FilesSearchingState == FilesSearchingState.PendingAI;
                if (isPendingAIState)
                {
                    ChatSessionComponent.FilesSearchingState = FilesSearchingState.DisplayingLatestStatus;
                    ChatSessionComponent.StateHasChanged();
                }

                if (assistantReplyStatus.Content != status)
                {
                    assistantReplyStatus.Content = status;
                    ChatSessionComponent.StateHasChanged();
                }

                return Task.CompletedTask;
            };

            Func<KnowledgeSource, Task> updateKnowledge = (loadedKnowledge) =>
            {
                if (loadedKnowledge == null)
                {
                    return Task.CompletedTask;
                }

                var isDisplayingLatestStatusState = ChatSessionComponent.FilesSearchingState == FilesSearchingState.DisplayingLatestStatus;
                if (isDisplayingLatestStatusState)
                {
                    ChatSessionComponent.FilesSearchingState = FilesSearchingState.SearchingFiles;
                }

                message.References.Add(loadedKnowledge);
                var originalFileName = loadedKnowledge.EmbededDocumentDto.OriginalFileName;
                var file = fileByFileName[originalFileName];
                var fileAdded = FilesBeingSearched.Add(file);
                if (fileAdded)
                {
                    ChatSessionComponent.StateHasChanged();
                }

                return Task.CompletedTask;
            };

            Func<string, bool, Task> updateBotResponse = (token, isDone) =>
            {
                if (string.IsNullOrEmpty(token))
                {
                    return Task.CompletedTask;
                }

                var isSearchingFilesState = ChatSessionComponent.FilesSearchingState == FilesSearchingState.SearchingFiles;
                if (isSearchingFilesState)
                {
                    ChatSessionComponent.FilesSearchingState = FilesSearchingState.ReplyingToUser;
                    this.ChatPrompts.Add(message);
                    ChatSessionComponent.StateHasChanged();
                }

                ChatSessionComponent.Mediator_StreamingResponseReceived((token, isDone));
                messageBuilder.Append(token);

                return Task.CompletedTask;
            };

            bot.StateUpdate = async (state) =>
            {
                var isDone = state.IsDone;
                await updateStatusText(state.StatusText);
                await updateKnowledge(state.Knowledge);
                await updateBotResponse(state.OutputToken, isDone);

                if (isDone)
                {
                    // for the case isError is true, expected to do the same things
                    ChatSessionComponent.FilesSearchingState = FilesSearchingState.None;
                    assistantReplyStatus.CustomType = AssistantReplyCustomerType.ResultStatus;
                    message.Content = messageBuilder.ToString();
                    message.IsStreaming = false;
                    ChatSessionComponent.StateHasChanged();

                    FilesBeingSearched = null;
                }
            };

            await bot.SubmitPrompt(query);

            WelcomePage.ChatBoxStrategy = new TextPromptStrategy(this);
            _workspaceFilesForSearch = null;
        });
    }

    private string PostProcessReferences(string message)
    {
        return Regex.Replace(
            message, 
            @"\[REF-(\d+)\]",
            match => { 
                int index = int.Parse(match.Groups[1].Value);
                return $"[[{index + 1}]](#)";
            });
    }

    public void OnPressTaskSearchForDocuments()
    {
        OnSwitchMainViewToChatView();
        OnOpenWorkspaceFileBrowserView(FileBrowserMode.SearchOnMultipleDocuments);
    }

    #endregion

    #region Switch MainView And Show/Hide WorkspaceFileBrowser

    public void OnToggleWorkspaceFileBrowserView()
    {
        var isFileBrowserActive = WorkspaceFileBrowserComponent.IsActive;
        if (isFileBrowserActive)
        {
            OnHideWorkspaceFileBrowserView();
        }
        else
        {
            OnOpenWorkspaceFileBrowserView(FileBrowserMode.None);
        }
    }

    public void OnOpenWorkspaceFileBrowserView(FileBrowserMode mode)
    {
        WorkspaceFileBrowserComponent.Open(mode);
        WelcomePage.DisplayState = DisplayState.LeftHalfView;
        WelcomePage.StateHasChanged();
    }

    public void OnHideWorkspaceFileBrowserView()
    {
        WorkspaceFileBrowserComponent.Close();
        WelcomePage.DisplayState = DisplayState.FullView;
        WelcomePage.StateHasChanged();
    }

    public void OnToggleMainView()
    {
        var isChatView = WelcomePage.MainView == MainView.ChatView;
        if (isChatView)
        {
            OnSwitchMainViewToWelcomeView();
        }
        else
        {
            OnSwitchMainViewToChatView();
        }
    }

    public void OnSwitchMainViewToChatView()
    {
        WelcomePage.MainView = MainView.ChatView;
        WelcomePage.StateHasChanged();
        WorkspaceFileBrowserComponent.IsToggleButtonVisible = true;
        WorkspaceFileBrowserComponent.StateHasChanged();
    }

    public void OnSwitchMainViewToWelcomeView()
    {
        WelcomePage.MainView = MainView.WelcomeView;
        WelcomePage.StateHasChanged();
        WorkspaceFileBrowserComponent.IsToggleButtonVisible = false;
        WorkspaceFileBrowserComponent.Close();
    }

    #endregion

    #region Components

    public Welcome WelcomePage { private get; set; }

    public ChatSession ChatSessionComponent { private get; set; }

    public WorkspaceFileBrowser WorkspaceFileBrowserComponent { private get; set; }

    #endregion

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
