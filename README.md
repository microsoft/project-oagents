> ⚠️ This project is still an experimentation phase and is not intended to be used in production yet.

# AI Agents Framework

An opinionated .NET framework, that is built on top of Semantic Kernel and Orleans, which helps creating and hosting event-driven AI Agents.

At the moment the library resides in `src/` only, but we plan to publish them as a Nuget Package in the future.

## Examples

We have created a few examples to help you get started with the framework and to explore its capabilities.

- [GitHub Dev Team Sample](samples/gh-flow/README.md): Build an AI Developer Team using event-driven agents, that help you automate the requirements engineering, planning, and coding process on GitHub.
- [Marketing Team Sample](samples/marketing/README.md): Create a marketing campaign using a content writer, graphic designer and social media manager.

- [Support center sample](samples/support-center/README.md): Model a call center team, each member is an expert in it's own domain and one is orchestrating the asks of the user based on the intent.

## FAQ

### What is this framework?

An opinionated .NET framework for creating and hosting **event-driven AI Agents**, built on:
- **Semantic Kernel** - Microsoft's AI orchestration SDK
- **Orleans** - Distributed actor framework for .NET

### Why event-driven agents?

Event-driven architecture allows agents to:
- Respond to external events (messages, user input, system events)
- Process asynchronously with better scalability
- Maintain state across distributed nodes
- Coordinate with other agents through event streams

### Is this production-ready?

⚠️ **Not yet** - This is still in experimentation phase. Use for:
- Learning event-driven agent patterns
- Prototyping agent systems
- Exploring Semantic Kernel + Orleans integration

### What .NET version is required?

.NET 8 or later (required by Orleans and Semantic Kernel).

### How do I get started?

1. Clone the repository
2. Explore the sample projects in `samples/`:
   - `gh-flow` - GitHub automation agents
   - `marketing` - Marketing campaign agents
   - `support-center` - Call center simulation

### What is Semantic Kernel?

Semantic Kernel is Microsoft's lightweight SDK that lets you:
- Connect AI models to your code
- Define skills and functions
- Orchestrate AI calls with planners

Learn more: [Semantic Kernel Documentation](https://learn.microsoft.com/semantic-kernel/)

### What is Orleans?

Orleans is a framework for building distributed systems using the **virtual actor model**:
- Actors (grains) are automatically managed
- Built-in state persistence
- Automatic scaling and recovery
- Ideal for distributed agent systems

Learn more: [Orleans Documentation](https://learn.microsoft.com/orleans/)

### Can I use other LLM providers?

Yes! Semantic Kernel supports:
- Azure OpenAI
- OpenAI.com
- Google Gemini
- Anthropic Claude
- Local models (via Ollama or other endpoints)

### How do agents communicate?

Through Orleans grains and event streams:
- Direct grain calls for synchronous requests
- Event streams for asynchronous messaging
- Observer patterns for pub/sub

### Where can I get help?

- Explore the sample code in `samples/`
- Check [Semantic Kernel docs](https://learn.microsoft.com/semantic-kernel/)
- Check [Orleans docs](https://learn.microsoft.com/orleans/)
- Open an issue in this repository

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit <https://cla.opensource.microsoft.com>.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Legal Notices

Microsoft and any contributors grant you a license to the Microsoft documentation and other content
in this repository under the [Creative Commons Attribution 4.0 International Public License](https://creativecommons.org/licenses/by/4.0/legalcode),
see the [LICENSE](LICENSE) file, and grant you a license to any code in the repository under the [MIT License](https://opensource.org/licenses/MIT), see the
[LICENSE-CODE](LICENSE-CODE) file.

Microsoft, Windows, Microsoft Azure and/or other Microsoft products and services referenced in the documentation
may be either trademarks or registered trademarks of Microsoft in the United States and/or other countries.
The licenses for this project do not grant you rights to use any Microsoft names, logos, or trademarks.
Microsoft's general trademark guidelines can be found at <http://go.microsoft.com/fwlink/?LinkID=254653>.

Privacy information can be found at <https://privacy.microsoft.com/en-us/>

Microsoft and any contributors reserve all other rights, whether under their respective copyrights, patents,
or trademarks, whether by implication, estoppel or otherwise.
