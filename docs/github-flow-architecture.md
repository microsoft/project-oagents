# Azure components

* SK Kernel Service – ASP.NET Core Service with REST API
* SK Skills:
  * PM Skill – generates pot, word docs, describing app,
  * Designer Skill – mockups?
  * Architect Skill – proposes overall arch
  * DevLead Skill – proposes task breakdown
  * CoderSkill – builds code modules for each task
  * ReviewerSkill – improves code modules
  * TestSkill – writes tests
  * Etc
* Web app: prompt front end and wizard style editor of app
* Build service sandboxes – using branches and actions/pipelines 1st draft; Alternate – ephemeral build containers
* Logging service streaming back to azure logs analytics, app insights, and teams channel
* Deployment service – actions/pipelines driven
* Azure Dev Skill – lean into azure integrations – crawl the azure estate to inventory a tenant’s existing resources to memory and help inform new code. Eg: you have a large azure sql estate? Ok, most likely you want to wire your new app to one of those dbs, etc….