> ⚠️ This project is still an experimentation phase and is not intended to be used in production yet.

# AI Agents Framework

An opinionated .NET framework, that is built on top of Semantic Kernel and Orleans, which helps creating and hosting event-driven AI Agents.

At the moment the library resides in `src/` only, but we plan to publish them as a Nuget Package in the future.

## Examples

We have created a few examples to help you get started with the framework and to explore its capabilities.

- [GitHub Dev Team Sample](samples/gh-flow/README.md): Build an AI Developer Team using event-driven agents, that help you automate the requirements engineering, planning, and coding process on GitHub.
- [Marketing Team Sample](samples/marketing/README.md): Create a marketing campaign using a content writer, graphic designer and social media manager.

- [Support center sample](samples/support-center/README.md): Model a call center team, each member is an expert in it's own domain and one is orchestrating the asks of the user based on the intent.

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

## ❓ FAQ

### What is this project?
This is an experimental .NET framework built on top of Semantic Kernel and Orleans for creating and hosting event-driven AI Agents. It helps you build multi-agent systems where each agent has its own domain expertise.

### Is this production-ready?
No. This project is still in an experimentation phase and is not intended for production use yet. The team plans to publish the library as a NuGet package in the future.

### What is the difference between Semantic Kernel and this framework?
Semantic Kernel provides the core AI integration primitives. This framework adds an opinionated architecture on top, specifically designed for event-driven multi-agent scenarios using Orleans for distributed computing.

### How do I get started?
Check out the [examples](samples/) directory:
- [GitHub Dev Team Sample](samples/gh-flow/README.md): AI Developer Team for requirements engineering and coding
- [Marketing Team Sample](samples/marketing/README.md): Content writer, designer, and social media manager agents
- [Support Center Sample](samples/support-center/README.md): Call center team with domain experts

### What is Orleans and why is it used?
Orleans is a cross-platform framework for building distributed applications. It provides virtual actors (grains) that make it easy to build scalable, stateful agent systems without managing complex concurrency.

### Can I use my own LLM models?
Yes. Since it is built on Semantic Kernel, you can configure any LLM provider that Semantic Kernel supports, including OpenAI, Azure OpenAI, and local models.

### How do agents communicate?
Agents communicate through events. Each agent is an independent grain that can publish and subscribe to events, enabling loose coupling and scalable multi-agent orchestration.

### What skills do the agents have?
Each agent in the samples has domain-specific skills. For example, the GitHub Dev Team includes agents for requirements analysis, planning, and coding. The Marketing Team includes content writer, graphic designer, and social media manager agents.

### How do I contribute?
Contributions are welcome! Most contributions require a Contributor License Agreement (CLA). See the [Contributing](#contributing) section above for details.

### Where can I get help?
- Open an [Issue](https://github.com/microsoft/project-oagents/issues) for bugs or feature requests
- Join the [Discussions](https://github.com/microsoft/project-oagents/discussions) community
- Contact [opencode@microsoft.com](mailto:opencode@microsoft.com) for questions about the CLA

