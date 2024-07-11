using Marketing.Controller;
using Marketing.Events;
using Marketing.Options;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;

namespace Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class CommunityManager : AiAgent<CommunityManagerState>
{
    protected override string Namespace => Consts.OrleansNamespace;

    private readonly ILogger<CommunityManager> _logger;

    public CommunityManager([PersistentState("state", "messages")] IPersistentState<AgentState<CommunityManagerState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<CommunityManager> logger)
    : base(state, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.UserConnected):
                // The user reconnected, let's send the last message if we have one
                string lastMessage = _state.State.History.LastOrDefault()?.Message;
                if (lastMessage == null)
                {
                    return;
                }

                await SendDesignedCreatedEvent(lastMessage, item.Data["UserId"]);
                break;

            case nameof(EventTypes.UserChatInput):
            case nameof(EventTypes.ArticleCreated):
                {
                    string article;
                    if (item.Data.ContainsKey("article"))
                    {
                        article = item.Data["article"];
                        _state.State.Data.Article = article;
                    }
                    else if (_state.State.Data.Article != null)
                    {
                        article = _state.State.Data.Article;
                    }
                    else
                    { 
                        // No article yet
                        return;
                    }

                    if (item.Data.ContainsKey("userMessage"))
                    {
                        article += "| USER REQUEST: " + item.Data["userMessage"];
                    }
                    _logger.LogInformation($"[{nameof(GraphicDesigner)}] Event {nameof(EventTypes.ArticleCreated)}. Article: {article}");

                    var context = new KernelArguments { ["input"] = AppendChatHistory(article) };
                    string socialMediaPost = await CallFunction(CommunityManagerPrompts.WritePost, context);
                    if (socialMediaPost.Contains("NOTFORME"))
                    {
                        return;
                    }
                    _state.State.Data.WrittenSocialMediaPost = socialMediaPost;

                    await SendDesignedCreatedEvent(socialMediaPost, item.Data["UserId"]);
                    break;
                }
            default:
                break;
        }
    }

    private async Task SendDesignedCreatedEvent(string socialMediaPost, string userId)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventTypes.SocialMediaPostCreated),
            Data = new Dictionary<string, string> {
                            { "UserId", userId },
                            { nameof(socialMediaPost), socialMediaPost}
                        }
        });
    }

    public Task<String> GetArticle()
    {
        return Task.FromResult(_state.State.Data.WrittenSocialMediaPost);
    }
}