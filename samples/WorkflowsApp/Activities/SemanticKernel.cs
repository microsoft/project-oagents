using Elsa.Extensions;
using JetBrains.Annotations;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.Models;
using Microsoft.SKDevTeam;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;


namespace Elsa.SemanticKernel;

/// <summary>
/// Invoke a Semantic Kernel skill.
/// </summary>
[Activity("Elsa", "Semantic Kernel", "Invoke a Semantic Kernel skill. ", DisplayName = "Generic Semantic Kernel Skill", Kind = ActivityKind.Task)]
[PublicAPI]
public class SemanticKernelSkill : CodeActivity<string>
{
    [Input(
    Description = "System Prompt",
    UIHint = InputUIHints.MultiLine,
    DefaultValue = PromptDefaults.SystemPrompt)]
    public Input<string> SysPrompt { get; set; } = default!;

    [Input(
    Description = "User Input Prompt",
    UIHint = InputUIHints.MultiLine,
    DefaultValue = PromptDefaults.UserPrompt)]
    public Input<string> Prompt { get; set; }

    [Input(
    Description = "Max retries",
    UIHint = InputUIHints.SingleLine,
    DefaultValue = KernelSettings.DefaultMaxRetries)]
    public Input<int> MaxRetries { get; set; }

    [Input(
    Description = "The skill to invoke from the semantic kernel",
    UIHint = InputUIHints.SingleLine,
    DefaultValue = "Chat")]
    public Input<string> SkillName { get; set; }

    [Input(
    Description = "The function to invoke from the skill",
    UIHint = InputUIHints.SingleLine,
    DefaultValue = "ChatCompletion")]
    public Input<string> FunctionName { get; set; }

    /*     [Input(
            Description = "Mockup - don't actually call the AI, just output the prompts",
            UIHint = InputUIHints.Checkbox,
            DefaultValue = false)]
        public Input<bool> Mockup { get; set; } */

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext workflowContext)
    {
        var test = SkillName.Get(workflowContext);
        var skillName = SkillName.Get(workflowContext);
        var functionName = FunctionName.Get(workflowContext);
        var systemPrompt = SysPrompt.Get(workflowContext);
        var maxRetries = MaxRetries.Get(workflowContext);
        var prompt = Prompt.Get(workflowContext);
        //var mockup = Mockup.Get(workflowContext);
        var mockup = false;

        string info = ($"#################\nSkill: {skillName}\nFunction: {functionName}\nPrompt: {prompt}\n#################\n\n");

        if (mockup)
        {
            workflowContext.SetResult(info);
        }
        else
        {
            // get the kernel
            var kernel = KernelBuilder.BuildKernel();

            // load the skill
            var promptTemplate = SemanticFunctionConfig.ForSkillAndFunction(skillName, functionName);

            var function = kernel.CreateFunctionFromPrompt(promptTemplate.PromptTemplate, new OpenAIPromptExecutionSettings { MaxTokens = 4096, Temperature = 0.4, TopP = 1 });

            // set the context (our prompt)
            var arguments =  new KernelArguments{
                ["input"] = prompt
            };

            var answer = await kernel.InvokeAsync(function, arguments);
            workflowContext.SetResult(answer);
        }
    }
}