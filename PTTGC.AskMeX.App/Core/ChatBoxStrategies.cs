using PTTGC.AskMeX.App.Core.Services;

namespace PTTGC.AskMeX.App.Core;

public interface IChatBoxStrategy
{
    Task Submit(string userInput);
}

public abstract class ChatBoxStrategy : IChatBoxStrategy
{
    protected readonly ChatSessionMediator _mediator;

    public ChatBoxStrategy(ChatSessionMediator mediator)
    {
        _mediator = mediator;
    }

    public abstract Task Submit(string userInput);
}

public class TextPromptStrategy : ChatBoxStrategy
{
    public TextPromptStrategy(ChatSessionMediator mediator) : base(mediator)
    {
    }

    public override Task Submit(string userInput)
        => _mediator.SendUserMessage(userInput);
}

public class SearchFromWorkspacePDFsStrategy : ChatBoxStrategy
{
    public SearchFromWorkspacePDFsStrategy(ChatSessionMediator mediator) : base(mediator)
    {
    }

    public override Task Submit(string userInput)
    {
        if (userInput == "Cancel")
        {
            _mediator.OnCancelSearchFromWorkspacePDFs();
            return Task.CompletedTask;
        }
        else
        {
            return _mediator.OnSubmitSearchQueryForWorkspacePDFs(userInput);
        }
    }
}