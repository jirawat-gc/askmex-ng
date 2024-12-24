using Microsoft.AspNetCore.Components;
using PTTGC.AskMeX.App.Core.Services;
using PTTGC.AskMeX.App.Core.Types;
using System.Text;

namespace PTTGC.AskMeX.App.Components;

public partial class WorkspaceFileBrowser
{
    private WorkspaceFile? _selectedFile;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Mediator.WorkspaceFileBrowserComponent = this;
    }

    public new void StateHasChanged()
    {
        base.StateHasChanged();
    }

    private void SelectFile(WorkspaceFile file)
    {
        _selectedFile = file;
    }

    private string GetFileClasses(WorkspaceFile file)
    {
        var classes = new StringBuilder();
        classes.Append("file");
        if (_selectedFile == file)
        {
            classes.Append(" selected");
        }
        return classes.ToString();
    }

    public void ClearSelectedFile()
    {
        _selectedFile = null;
    }

    [Inject]
    public required ChatSessionMediator Mediator { private get; init; }

    public bool IsActive { get; set; } = false;

    // TODO: visible when (CurrentPrimaryView == ChatView)
    public bool IsToggleButtonVisible { get; set; }

    private string ConfirmBtnClasses
    {
        get
        {
            if (_selectedFile == null)
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
