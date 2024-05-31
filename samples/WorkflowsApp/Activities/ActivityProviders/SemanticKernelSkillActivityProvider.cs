using System.Reflection;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SKDevTeam;

namespace Elsa.SemanticKernel;

//<summary>
// Loads the Semantic Kernel skills and then generates activites for each skill
//</summary>
public class SemanticKernelActivityProvider : IActivityProvider
{
    private readonly IActivityFactory _activityFactory;
    private readonly IActivityDescriber _activityDescriber;

    public SemanticKernelActivityProvider(IActivityFactory activityFactory, IActivityDescriber activityDescriber)
    {
        _activityFactory = activityFactory;
        _activityDescriber = activityDescriber;
    }
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        // get the kernel
        var kernel = KernelBuilder.BuildKernel();

        // get a list of skills in the assembly
        var skills = LoadSkillsFromAssemblyAsync(typeof(SemanticKernelActivityProvider).Assembly.ToString(), kernel);

        // create activity descriptors for each skilland function
        var activities = new List<ActivityDescriptor>();
        foreach (var skill in skills)
        {
            Console.WriteLine($"Creating Activities for Plugin: {skill.SemanticFunctionConfig.SkillName}");
            activities.Add(await CreateActivityDescriptorFromSkillAndFunction(skill, cancellationToken));
        }

        return activities;
    }

    /// <summary>
    /// Creates an activity descriptor from a skill and function.
    /// </summary>
    /// <param name="function">The semantic kernel function</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>An activity descriptor.</returns>
    private async Task<ActivityDescriptor> CreateActivityDescriptorFromSkillAndFunction(SkillDefintion skill, CancellationToken cancellationToken = default)
    {
        // Create a fully qualified type name for the activity 
        var thisNamespace = GetType().Namespace;
        var function = skill.SemanticFunctionConfig;

        var fullTypeName = $"{thisNamespace}.{function.SkillName}.{function.Name}";
        Console.WriteLine($"Creating Activity: {fullTypeName}");

        // create inputs from the function parameters - the SemanticKernelSkill activity will be the base for each activity
        var inputs = new List<InputDescriptor>();
        
        foreach (var p in skill.KernelFunction.Metadata.Parameters)
        {
            inputs.Add(CreateInputDescriptorFromSKParameter(p));
        }

        inputs.Add(await _activityDescriber.DescribeInputPropertyAsync<SemanticKernelSkill, Input<int>>(x => x.MaxRetries, cancellationToken));

        var outputDescriptor = await _activityDescriber.DescribeOutputProperty<SemanticKernelSkill, Output<string>?>(x => x.Result, cancellationToken);

        return new ActivityDescriptor
        {
            Kind = ActivityKind.Task,
            Category = "Semantic Kernel",
            Description = function.Description,
            Name = function.Name,
            TypeName = fullTypeName,
            Namespace = $"{thisNamespace}.{function.SkillName}",
            DisplayName = $"{function.SkillName}.{function.Name}",
            Inputs = inputs,
            Outputs = new[] { outputDescriptor },
            Constructor = context =>
            {
                // The constructor is called when an activity instance of this type is requested.

                // Create the activity instance.
                var activityInstance = _activityFactory.Create<SemanticKernelSkill>(context);

                // Customize the activity type name.
                activityInstance.Type = fullTypeName;

                // Configure the activity's URL and method properties.
                activityInstance.SkillName = new Input<string?>(function.SkillName);
                activityInstance.FunctionName = new Input<string?>(function.Name);
                activityInstance.SysPrompt = new Input<string?>(function.PromptTemplate);
                activityInstance.Prompt = new Input<string?>(String.Empty);

                return activityInstance;
            }
        };

    }
    /// <summary>
    /// Creates an input descriptor for a single line string
    /// </summary>
    /// <param name="name">The name of the input field</param>
    /// <param name="description">The description of the input field</param>
    private InputDescriptor CreateInputDescriptor(Type inputType, string name, Object defaultValue, string description)
    {
        var inputDescriptor = new InputDescriptor
        {
            Description = description,
            DefaultValue = defaultValue,
            Type = inputType,
            Name = name,
            DisplayName = name,
            IsSynthetic = true, // This is a synthetic property, i.e. it is not part of the activity's .NET type.
            IsWrapped = true, // This property is wrapped within an Input<T> object.
            UIHint = InputUIHints.SingleLine,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(name),
            ValueSetter = (activity, value) => activity.SyntheticProperties[name] = value!,
        };
        return inputDescriptor;
    }

    /// <summary>
    /// Creates an input descriptor from an sk funciton parameter definition.
    /// </summary>
    /// <param name="parameter">The function parameter.</param>
    /// <returns>An input descriptor.</returns>
    private InputDescriptor CreateInputDescriptorFromSKParameter(KernelParameterMetadata parameter)
    {
        var inputDescriptor = new InputDescriptor
        {
            Description = string.IsNullOrEmpty(parameter.Description) ? parameter.Name : parameter.Description,
            DefaultValue = parameter.DefaultValue == null ? string.Empty : parameter.DefaultValue,
            Type = typeof(string),
            Name = parameter.Name,
            DisplayName = parameter.Name,
            IsSynthetic = true, // This is a synthetic property, i.e. it is not part of the activity's .NET type.
            IsWrapped = true, // This property is wrapped within an Input<T> object.
            UIHint = InputUIHints.MultiLine,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(parameter.Name),
            ValueSetter = (activity, value) => activity.SyntheticProperties[parameter.Name] = value!,

        };
        return inputDescriptor;
    }

    ///<summary>
    /// Gets a list of the skills in the assembly
    ///</summary>
    private IEnumerable<SkillDefintion> LoadSkillsFromAssemblyAsync(string assemblyName, Kernel kernel)
    {
        var skills = new List<SkillDefintion>();
        var assembly = Assembly.Load(assemblyName);
        Type[] skillTypes = assembly.GetTypes()
            .Where(type => type.Namespace == "Microsoft.SKDevTeam")
            .ToArray();
        foreach (Type skillType in skillTypes)
        {

            var functions = skillType.GetFields();
            foreach (var function in functions)
            {
                string field = function.FieldType.ToString();
                if (field.Equals("Microsoft.SKDevTeam.SemanticFunctionConfig"))
                {
                    var promptTemplate = SemanticFunctionConfig.ForSkillAndFunction(skillType.Name, function.Name);
                    var skfunc = kernel.CreateFunctionFromPrompt(
                        promptTemplate.PromptTemplate, new OpenAIPromptExecutionSettings
                        {
                            MaxTokens = promptTemplate.MaxTokens,
                            Temperature = promptTemplate.Temperature,
                            TopP = promptTemplate.TopP
                        });

                    skills.Add(new SkillDefintion
                    {
                        SemanticFunctionConfig = promptTemplate,
                        KernelFunction = skfunc
                    });

                    Console.WriteLine($"SKActivityProvider Added SK function: {skillType.Name}.{function.Name}");
                }
            }
        }
        return skills;
    }
}

internal class SkillDefintion
{
    public SemanticFunctionConfig SemanticFunctionConfig { get; set; }
    public KernelFunction KernelFunction { get; set; }
}

