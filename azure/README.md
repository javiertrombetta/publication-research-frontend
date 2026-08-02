# Azure

`deploy.sh` puts this application on Azure Container Apps, into the same environment as the API.

Run the API's deploy script first: this one reads the API's address out of that deployment rather
than asking for it, so the two cannot drift apart.

```bash
az login
az account set --subscription "Azure for Students"
./azure/deploy.sh
```

The full walkthrough, the costs and what to do afterwards are in the API repository, at
`docs/azure.md`. It covers both applications because they are deployed together.
