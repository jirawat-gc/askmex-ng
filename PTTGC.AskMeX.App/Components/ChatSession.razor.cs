using Markdig;
using Microsoft.AspNetCore.Components;
using PTTGC.AskMeGc;
using PTTGC.AskMeGc.OpenAI;

namespace PTTGC.AskMeX.App.Components;

public partial class ChatSession
{
    private OpenAIChatSession Session
    {
        get
        {
            // list out Role
            // list out CustomType
            return new()
            {
                ChatPrompts = new()
                {
                    new()
                    {
                        CustomId = "0",
                        CustomType = "",
                        Role = "user",
                        RenderedContent = "What is the latest news on South Korea Election for 2022?",
                        References = new()
                    },
                    new()
                    {
                        CustomId = "1",
                        CustomType = "function_call",
                        Role = "system",
                        RenderedContent = "Search via internet",
                        References = new()
                    },
                    new()
                    {
                        CustomId = "2",
                        CustomType = "",
                        Role = "assistant",
                        RenderedContent = "#### Current status on Nov 2022\r\n\r\nThe South Korean presidential election took place on March 9, 2022. Yoon Suk-yeol, the candidate from the conservative People Power Party, won the election, defeating Lee Jae-myung of the Democratic Party. Yoon's victory marked a shift in the political landscape, as he focused on issues like economic reform, national security, and a tougher stance on North Korea.[^first]\r\n\r\nThe election was noted for its high voter turnout and the influence of social media in campaigning. Following the election, Yoon was inaugurated as the 13th president of South Korea on May 10, 2022. His administration has faced various challenges, including economic issues and tensions with North Korea.\r\n\r\nFor more specific developments or updates after that time, you may want to consult news archives or reliable sources.\r\n\r\n__Advertisement__\r\n\r\n- __[pica](https://nodeca.github.io/pica/demo/)__ - high quality and fast image\r\n  resize in browser.\r\n- __[babelfish](https://github.com/nodeca/babelfish/)__ - developer friendly\r\n  i18n with plurals support and easy syntax.",
                        References = new()
                        {
                            new()
                            {
                                IsUseful = true,
                                Usefulness = 1,
                                Certainty = 0.5,
                            },
                            new()
                            {
                                IsUseful = false,
                                Usefulness = 3,
                                Certainty = 0.7,
                            }
                        }
                    }
                }
            };
        }
    }

    private MarkdownPipeline pipeline;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        }
    }

    /// <summary>
    /// Renders markdown to HTML
    /// </summary>
    /// <param name="markdown"></param>
    /// <returns></returns>
    MarkupString RenderMarkdown(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return (MarkupString)string.Empty;
        }

        return (MarkupString)Markdig.Markdown.ToHtml(markdown, pipeline!);
    }

    public void SendMessage(string message)
    {
        Session.AddNewUserPrompt(message);
    }
}
