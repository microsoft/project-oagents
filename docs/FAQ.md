# Frequently Asked Questions (FAQ)

> **⚠️ Note:** This project is still in experimentation phase and is not intended for production use yet.

## Getting Started

### What is Project OAgents?

Project OAgents is an **opinionated .NET framework** built on top of Semantic Kernel and Orleans for creating and hosting **event-driven AI Agents**. It provides a structured approach to building multi-agent systems with clear responsibilities and workflows.

### How does OAgents differ from other AI agent frameworks?

| Feature | OAgents | LangChain | AutoGen | CrewAI |
|---------|---------|-----------|---------|--------|
| Language | .NET (C#) | Python | Python | Python |
| Architecture | Event-driven | Chain-based | Conversation-based | Role-based |
| Runtime | Orleans (distributed) | Python process | Python process | Python process |
| State Management | Persistent (Orleans grains) | In-memory | In-memory | In-memory |
| Deployment | Azure-ready | Container | Container | Container |
| Production Status | Experimental | Stable | Stable | Stable |

**Key advantages of OAgents:**
- **.NET ecosystem**: Leverages C#, Orleans, and Azure services
- **Event-driven**: Clear agent coordination via events (no hidden chains)
- **Distributed by design**: Orleans provides built-in scaling and persistence
- **Azure integration**: First-class Azure services support (Event Hub, CosmosDB, etc.)

### What are the prerequisites?

- **.NET 8.0** or later
- **Docker** (for devcontainer setup)
- **Azure account** (optional, for cloud deployment)
- **GitHub account** (for webhook-based samples)
- **VS Code** with devcontainer extension (recommended)

### How do I get started locally?

1. **Clone and open in devcontainer:**
   ```bash
   git clone https://github.com/microsoft/project-oagents.git
   cd project-oagents
   # Open in VS Code and accept devcontainer prompt
   ```

2. **Or use Codespaces:**
   - Click "Code" → "Codespaces" → "Create codespace"

3. **Run a sample:**
   ```bash
   cd samples/gh-flow
   # Follow the getting started guide
   ```

## Core Concepts

### What is event-driven agent architecture?

Agents communicate via **events** rather than direct calls. Each agent:
- **Subscribes** to specific events
- **Processes** events independently
- **Emits** new events for other agents

This creates a clean separation of concerns and makes the system more maintainable and observable.

### What are the core components?

| Component | Package | Purpose |
|-----------|---------|---------|
| **Oagents.Core** | Core library | Agent base classes, event definitions |
| **Oagents.Orleans** | Orleans runtime | Distributed actor model, persistence |
| **Oagents.Dapr** | Dapr runtime | Alternative distributed runtime |

### What agent types are available in the samples?

| Agent | Role | Responsibilities |
|-------|------|------------------|
| **Product Manager** | Planning | Creates README, defines requirements, coordinates with user |
| **Developer Lead** | Architecture | Breaks down tasks, creates development plan |
| **Developer** | Implementation | Writes code, runs in sandbox, commits changes |
| **Orchestrator (Hubber)** | Coordination | Routes events, manages GitHub integration |
| **Azure Genie** | Infrastructure | Stores artifacts, manages blob storage |

### What events are used in the GitHub Dev Team sample?

- **NewAsk** → User creates a new request
- **ReadmeRequested/Generated/Created** → Product Manager workflow
- **DevPlanRequested/Generated/Created** → Developer Lead workflow
- **CodeGenerationRequested/Generated/Created** → Developer workflow
- **SandboxRunCreated/Finished** → Code execution in isolated environment

## Architecture & Infrastructure

### What communication mechanisms are available?

| Environment | Communication | Storage |
|-------------|---------------|---------|
| **Fully local** | Memory streams | Local storage |
| **With Azure** | Azure Event Hub | Azure CosmosDB |

Both can be extended to use other solutions/services.

### What is Dev Tunnels?

Dev Tunnels creates a **secure tunnel** for webhooks to send messages to your local system or Codespace environment. This enables GitHub webhooks to reach your local development setup without exposing public endpoints.

Learn more: [Dev Tunnels documentation](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview)

### What containers are used in devcontainer setup?

- **Devcontainer** → Development environment
- **Qdrant** → Vector database for semantic search
- **Cosmos emulator** → Local CosmosDB for chat history

### How is chat history stored?

- **CosmosDB** (production or emulator)
- Can be extended to other storage solutions

## Deployment

### Can I deploy to Azure?

Yes! Use **Azure Developer CLI (`azd`)** to deploy Azure services:

```bash
azd up
```

Learn more: [Azure Developer CLI overview](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/overview)

### What Azure services are used?

| Service | Purpose |
|---------|---------|
| **Azure Event Hub** | Agent communication |
| **Azure CosmosDB** | Chat history storage |
| **Azure Blob Storage** | Artifact storage (README, code) |
| **Azure Functions** (optional) | Webhook handlers |
| **Azure Container Apps** (optional) | Agent hosting |

### Can I run without Azure?

Yes! The framework supports:
- **Fully local**: Memory streams + local storage + Cosmos emulator
- **Hybrid**: Some Azure services, some local

## Examples

### What samples are available?

| Sample | Description | Agents |
|--------|-------------|--------|
| **GitHub Dev Team** | Automate requirements, planning, coding on GitHub | PM, DevLead, Developer |
| **Marketing Team** | Create marketing campaigns | Content Writer, Graphic Designer, Social Media Manager |
| **Support Center** | Model a call center team | Domain experts + Orchestrator |
| **WorkflowsApp** | General workflow examples | Various |

### How does the GitHub Dev Team sample work?

1. **User creates issue** with natural language request
2. **Product Manager** generates README, iterates with user via comments
3. **Developer Lead** creates development plan, breaks down tasks
4. **Developer** writes code, runs in sandbox, commits to PR
5. **User reviews and approves** each step

Full workflow: See [GitHub flow getting started](./samples/gh-flow/docs/github-flow-getting-started.md)

### Can I create custom agents?

Yes! Extend the base agent classes:

```csharp
public class MyCustomAgent : AgentBase
{
    public override async Task HandleEvent(Event event)
    {
        // Custom logic
        await Emit(new MyCustomEvent(result));
    }
}
```

## Troubleshooting

### Devcontainer won't start

- Ensure Docker is running
- Check VS Code has devcontainer extension
- Try recreating the container: "Rebuild Container"

### GitHub webhooks not received

- Verify Dev Tunnel is running
- Check GitHub webhook URL uses tunnel endpoint
- Inspect tunnel logs for errors

### Cosmos emulator connection issues

- Wait for emulator to fully start (may take 2-3 minutes)
- Check container logs: `docker logs cosmos-emulator`
- Verify connection string in configuration

### Agent events not flowing

- Check Event Hub (or memory stream) is configured
- Inspect agent subscriptions
- Review event definitions match agent expectations

### Sandbox runs failing

- Verify sandbox environment has required dependencies
- Check code output is valid
- Review sandbox logs

## Contributing

See [Contributing Guidelines](../README.md#contributing) and [CODE_OF_CONDUCT.md](../CODE_OF_CONDUCT.md).

## More Information

- **Devcontainer/Codespaces**: Everything pre-installed
- **Azure Developer CLI**: Easy Azure deployment
- **Dev Tunnels**: Secure webhook routing
- **Communication**: Memory streams (local) or Event Hub (Azure)
- **Storage**: CosmosDB or emulator

## Need Help?

- Open an issue on [GitHub](https://github.com/microsoft/project-oagents/issues)
- Check the [samples](../samples/) for working examples
- Review the [documentation](../docs/)

---

**Microsoft Open Source Code of Conduct** | [FAQ](https://opensource.microsoft.com/codeofconduct/faq/) | [Contact](mailto:opencode@microsoft.com)