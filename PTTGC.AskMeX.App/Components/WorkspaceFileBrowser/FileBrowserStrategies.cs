using PTTGC.AskMeX.App.Core.Services;
using PTTGC.AskMeX.App.Core.Types;

namespace PTTGC.AskMeX.App.Components;

public partial class WorkspaceFileBrowser
{
    private interface IFileBrowserStrategy
    {
        void ClearSelectedFiles();
        bool IsSelected(WorkspaceFile file);
        void ToggleFileSelection(WorkspaceFile file);
        bool IsReadyToSummit { get; }
        Task Summit();
    }

    private class OnePdfToSummarizeStrategy : IFileBrowserStrategy
    {
        private readonly ChatSessionMediator _mediator;
        private WorkspaceFile? _selectedFile;

        public OnePdfToSummarizeStrategy(ChatSessionMediator mediator)
        {
            _mediator = mediator;
        }

        public bool IsReadyToSummit
            => _selectedFile != null;

        public void ClearSelectedFiles()
            => _selectedFile = null;

        public bool IsSelected(WorkspaceFile file)
            => _selectedFile == file;

        public void ToggleFileSelection(WorkspaceFile file)
        {
            if (_selectedFile == file)
            {
                _selectedFile = null;
            }
            else
            {
                _selectedFile = file;
            }
        }

        public Task Summit()
            => _mediator.OnConfirmToUseSelectedWorkspaceFileToSummarize(_selectedFile!);
    }

    private class SearchOnMultipleDocumentsStrategy : IFileBrowserStrategy
    {
        private readonly ChatSessionMediator _mediator;
        private readonly HashSet<WorkspaceFile> _selectedFiles;
        public SearchOnMultipleDocumentsStrategy(ChatSessionMediator mediator)
        {
            _mediator = mediator;
            _selectedFiles = new(new WorkspaceFileBlobNameComparer());
        }

        public bool IsReadyToSummit
            => _selectedFiles.Count > 0;

        public void ClearSelectedFiles()
            => _selectedFiles.Clear();

        public bool IsSelected(WorkspaceFile file)
            => _selectedFiles.Contains(file);

        public void ToggleFileSelection(WorkspaceFile file)
        {
            if (_selectedFiles.Contains(file))
            {
                _selectedFiles.Remove(file);
            }
            else
            {
                _selectedFiles.Add(file);
            }
        }

        public Task Summit()
            => _mediator.OnConfirmToUseSelectedWorkspaceFilesToSearch(_selectedFiles);
    }
}