using Marketing.Events;
using Marketing.Options;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.Extensions.AI;

namespace Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class CommunityManager([PersistentState("state", "messages")] IPersistentState<AgentState<CommunityManagerState>> state, IChatClient chatClient, ILogger<CommunityManager> logger) : AiAgent<CommunityManagerState>(state)
{
    protected override string Namespace => Consts.OrleansNamespace;

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
                    logger.LogInformation($"[{nameof(GraphicDesigner)}] Event {nameof(EventTypes.ArticleCreated)}. Article: {article}");

                    var input = AppendChatHistory(article);
                    var prompt = $"""
                                    You are a Marketing community manager writer.
                                    If the request from the user is to write a post to promote a new product, or if it is specifically talking to you (community manager) 
                                    then you should write a post based on the user request
                                    Your writings are going to be posted on Tweeter. So be informal, friendly and add some hashtags and emojis.
                                    Remember, the tweet cannot be longer than 280 characters.
                                    If the request was not intedend for you. reply with <NOTFORME>"
                                    ---
                                    Input: {input}
                                    ---
                     """;
                    var result = await chatClient.CompleteAsync(prompt);
                    var socialMediaPost = result.Message.Text!;
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