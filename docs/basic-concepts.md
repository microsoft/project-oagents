# Basic concepts

The main primitives that this framework defines are:

#### Agents

In its most basic deifnition, an agent is an entity that handles and publishes events.

### AiAgents

AiAgents are Agents that use AI models to execute their tasks.

#### Events

Domain defined events, that are used to build the workflow of agents.
Loosely based on CloudEvents, they are explicit, but we can allow for the Agents to create novel event types.

#### Knowledge

Retrieved information (usually from a vector DB), which will be useful for the task of the AiAgent.

#### Memory

The event (chat) history for the AiAgents.

#### Implementation details

We have two implementations, both using Actors.
The samples are using the Orleans implementation, but there is a Dapr actors sample of the dev team app.

#### Workflows

Explicit by defining events and Agents who handle those events.
We can add dynamic behavior by allowing the Agents to create a novel type of event and using unbounded type of Agents which can try to handle the new event.

#### Why Actors (Orleans)?

Actor model maps really well to agents
Streaming allows for modeling eventing topologies and partitioning
Clustered and distributed (can elastically scale and be re-distributed as well as deactivated after a period of inactivity)
Global unique identity
Single threaded execution
No need to worry about lifecycle (runtime takes care of that)
Support different plugin adapters for storage, streaming, etc.




