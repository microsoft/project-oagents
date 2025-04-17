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

# VGI notes

## Missing devtunnel
```shell
curl -sL https://aka.ms/DevTunnelCliInstall | bash
source ~/.profile [or ~/.zshrc]
devtunnel user login
devtunnel host gh-flow-demo
```
Update webhook at https://github.com/settings/apps/demo-gh-flow-app
with https://xxxxxxxxx-xxxx.uks1.devtunnels.ms/api/github/webhooks

Debug webhook at https://github.com/settings/apps/demo-gh-flow-app/advanced

## Run app

Under solution explorer: Right-click OAgents>samples>Microsoft.AI.DevTeam>Debug>Start an new instance

## Create issue

Todo scaffold

I’d like to build a typical Todo List Application: a simple productivity tool that allows users to create, manage, and track tasks or to‑do items.
Key features of the Todo List application include the ability to add, edit, and delete tasks, set due dates and reminders, categorize tasks by project or priority, and mark tasks as complete.
The Todo List application also offer collaboration features, such as sharing tasks with others or assigning tasks to team members.
Additionally, the Todo List application will offer mobile and web‑based interfaces, allowing users to access their tasks from anywhere.
Use C# as the language.
The app needs to be deployed to Azure, be highly performant, cost effective and secure, following the rules of Well Architected Framework.