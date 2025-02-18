using Marketing.Events;
using Marketing.Options;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.Extensions.AI;

namespace Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Writer([PersistentState("state", "messages")] IPersistentState<AgentState<WriterState>> state, IChatClient chatClient, ILogger<GraphicDesigner> logger) : AiAgent<WriterState>(state), IWriter
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

                await SendArticleCreatedEvent(lastMessage, item.Data["UserId"]);

                break;

            case nameof(EventTypes.UserChatInput):
                {
                    var userMessage = item.Data["userMessage"];
                    logger.LogInformation($"[{nameof(GraphicDesigner)}] Event {nameof(EventTypes.UserChatInput)}. UserMessage: {userMessage}");

                    var input = AppendChatHistory(userMessage);
                    var prompt = $"""
                                This is a multi agent app. You are a Marketing Campaign writer Agent.
                                If the request is not for you, answer with <NOTFORME>.
                                If the request is about writing or modifying an existing campaing, then you should write a campain based on the user request.
                                Write up to three paragraphs to promote the product the user is asking for.
                                Bellow are a series of inputs from the user that you can use.
                                If the input talks about twitter or images, dismiss it and return <NOTFORME>
                                Input: {input}
                                """;
                    var result = await chatClient.GetResponseAsync(prompt);
                    var newArticle = result.Message.Text!;

                    if (newArticle.Contains("NOTFORME"))
                    {
                        return;
                    }
                    await SendArticleCreatedEvent(newArticle, item.Data["UserId"]);
                    break;
                }
            case nameof(EventTypes.AuditorAlert):
                {
                    var auditorAlertMessage = item.Data["auditorAlertMessage"];
                    logger.LogInformation($"[{nameof(GraphicDesigner)}] Event {nameof(EventTypes.AuditorAlert)}. auditorAlertMessage: {auditorAlertMessage}");

                    var input = AppendChatHistory(auditorAlertMessage);
                    var prompt = $"""
                                    This is a multi agent app. You are a Marketing Campaign writer Agent.
                                    If the request is not for you, answer with <NOTFORME>.
                                    If the request is about writing or modifying an existing campaing, then you should write a campain based on the user request.
                                    The campaign is not compliant with the company policy, and you need to adjust it. This is the message from the automatic auditor agent regarding what is wrong with the original campaing
                                    ---
                                    Input: {input}
                                    ---
                                    Return only the new campaign text but adjusted to the auditor request
                                    """;
                    var result = await chatClient.GetResponseAsync(prompt);
                    var newArticle = result.Message.Text!;

                    if (newArticle.Contains("NOTFORME"))
                    {
                        return;
                    }
                    await SendArticleCreatedEvent(newArticle, item.Data["UserId"]);
                    break;
                }
            default:
                break;
        }
    }

    private async Task SendArticleCreatedEvent(string article, string userId)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventTypes.ArticleCreated),
            Data = new Dictionary<string, string> {
                            { "UserId", userId },
                            { nameof(article), article },
                        }
        });
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventTypes.AuditText),
            Data = new Dictionary<string, string> {
                            { "UserId", userId },
                            { "text", "Article writen by the Writer: " + article },
                        }
        });
    }

    public Task<String> GetArticle()
    {
        return Task.FromResult(_state.State.Data.WrittenArticle);
    }
}