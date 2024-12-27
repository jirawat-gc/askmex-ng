using Microsoft.AspNetCore.Components;
using PTTGC.AskMeX.App.Core.Services;
using PTTGC.AskMeX.App.Core.Types;
using System.Text;

namespace PTTGC.AskMeX.App.Components;

public partial class WorkspaceFileBrowser
{
    private IFileBrowserStrategy? _modeStrategy;
    private Dictionary<FileBrowserMode, IFileBrowserStrategy?> _strategyByMode;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Mediator.WorkspaceFileBrowserComponent = this;
        _strategyByMode= new()
        {
            { FileBrowserMode.None, null },
            { FileBrowserMode.OnePdfToSummarize, new OnePdfToSummarizeStrategy(Mediator) },
            { FileBrowserMode.SearchOnMultipleDocuments, new SearchOnMultipleDocumentsStrategy(Mediator) }
        };
    }

    public new void StateHasChanged()
    {
        base.StateHasChanged();
    }

    private string GetFileClasses(WorkspaceFile file)
    {
        var classes = new StringBuilder();
        classes.Append("file");
        var isSelected = _modeStrategy?.IsSelected(file) ?? false;
        if (isSelected)
        {
            classes.Append(" selected");
        }
        return classes.ToString();
    }

    public void ClearSelectedFile()
    {
        _modeStrategy!.ClearSelectedFiles();
    }

    public void Open(FileBrowserMode mode)
    {
        Mode = mode;
        _modeStrategy = _strategyByMode[mode];
        IsActive = true;
        base.StateHasChanged();
    }

    public void Close()
    {
        Mode = FileBrowserMode.None;
        _modeStrategy?.ClearSelectedFiles();
        IsActive = false;
        base.StateHasChanged();
    }

    [Inject]
    public required ChatSessionMediator Mediator { private get; init; }

    public FileBrowserMode Mode { get; private set; }
        = FileBrowserMode.None;

    public bool IsActive { get; private set; }
        = false;

    public bool IsToggleButtonVisible { get; set; }

    private string ConfirmBtnClasses
    {
        get
        {
            var isReadyForSummit = _modeStrategy?.IsReadyToSummit ?? false;
            if (!isReadyForSummit)
            {
                return "disabled";
            }

            return "";
        }
    }

    private string OpenWorkspaceFileBrowserButtonClasses
    {
        get
        {
            var classes = new StringBuilder();
            classes.Append("open-workspace-file-broswer-btn");
            if (IsToggleButtonVisible) 
            {
                classes.Append(" visible");
            }
            if (IsActive)
            {
                classes.Append(" workspace-file-browser-active");
            }
            return classes.ToString();
        }
    }

    private string WrokspaceFileBrowserViewClasses
    {
        get
        {
            var classes = new StringBuilder();
            classes.Append("workspace-file-browser");
            if (IsActive)
            {
                classes.Append(" active");
            }
            return classes.ToString();
        }
    }
}

public enum FileBrowserMode
{
    None,
    OnePdfToSummarize,
    SearchOnMultipleDocuments
}
