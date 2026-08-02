# Azure

`deploy.sh` puts this application on Azure Container Apps, into the same environment as the API.

Run the API's deploy script first: this one reads the API's address out of that deployment rather
than asking for it, so the two cannot drift apart.

```bash
az login
az account set --subscription "Azure for Students"
./azure/deploy.sh
```

`deploy-image.sh` is the smaller one: it points the running application at the image built from the
commit you are standing on and changes nothing else. Every push to `main` already does this on its
own, so this is for putting a specific image up by hand, or for a commit built before that was
switched on.

```bash
./azure/deploy-image.sh              # the current commit
./azure/deploy-image.sh sha-abc1234  # or a tag you name
```

It refuses a tag that was never built, rather than leaving the application pointing at an image that
does not exist, and it waits until the new revision is the one actually serving traffic before
saying it is done.

The full walkthrough, the costs, how GitHub signs in to Azure and what to do afterwards are in the
API repository, at `docs/azure.md`. It covers both applications because they are deployed together.
