## Prerequisites

- Access to gpt3.5-turbo or preferably gpt4 - [Get access here](https://learn.microsoft.com/en-us/azure/ai-services/openai/overview#how-do-i-get-access-to-azure-openai)
- [Setup a Github app](#how-do-i-setup-the-github-app)
- [Install the Github app](https://docs.github.com/en/apps/using-github-apps/installing-your-own-github-app)
- [Create labels for the dev team skills](#which-labels-should-i-create)

### How do I setup the Github app?

- [Register a Github app](https://docs.github.com/en/apps/creating-github-apps/registering-a-github-app/registering-a-github-app).
- Setup the following permissions
    - Repository 
        - Contents - read and write
        - Issues - read and write
        - Metadata - read only
        - Pull requests - read and write
- Subscribe to the following events:
    - Issues
    - Issue comment
- Allow this app to be installed by any user or organization
- Add a dummy value for the webhook url, we'll come back to this setting
- After the app is created, generate a private key, we'll use it later for authentication to Github from the app

### Which labels should I create?

In order for us to know which skill and persona we need to talk with, we are using Labels in Github Issues

The default bunch of skills and personnas are as follows:
- PM.Readme
- PM.BootstrapProject
- Do.It
- DevLead.Plan
- Developer.Implement

Once you start adding your own skills, just remember to add the corresponding Label!

## How do I run this locally?

Codespaces are preset for this repo.

Create a codespace and once the codespace is created, make sure to fill in the `local.settings.json` file.

There is a `local.settings.template.json` you can copy and fill in, containing comments on the different config values.

Hit F5 and go to the Ports tab in your codespace, make sure you make the `:7071` port publically visible. [How to share port?](https://docs.github.com/en/codespaces/developing-in-codespaces/forwarding-ports-in-your-codespace?tool=vscode#sharing-a-port-1)

Copy the local address (it will look something like https://foo-bar-7071.preview.app.github.dev) and append `/api/github/webhooks` at the end. Using this value, update the Github App's webhook URL and you are ready to go!


## How do I deploy this to Azure?

```bash
azd auth login
azd up
```