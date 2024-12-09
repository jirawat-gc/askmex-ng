namespace PTTGC.AskMeX.App.Core.Types;

// NOTE [YO] : has not been using this class as this code was duplicated from AskMeGC Project
// might need to remove this class when clean up along with AskMeGCPage<T> class
public class SessionListItem
{
    public required string Id { get; set; }

    public required DateTime Updated { get; set; }

    public required string Title { get; set; }
}
