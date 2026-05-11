# Requirements for the Integartion with Azure DevOpsare described here:

## Azure Boards GitHub App (strongly recommended)
This is best‑supported approach.
What it gives you

Link GitHub issues & PRs to Azure Boards
Auto‑close work items when PRs merge
Bi‑directional status updates (state‑only)
No tokens, no scripts, no maintenance

## Azure Boards Integration with GitHub

Limitation:

It links, it does not fully “mirror” issues.
Fields like description edits and comments are not fully synced.

✅ Perpare the Linking of the repo with Azure DevOps and what to do now (exact order)

✅ Go to GitHub → Settings → Secrets → Actions
✅ Confirm AZDO_REPO_URL and AZDO_PAT exists and is not empty

✅ Either:

keep your existing workflow or
switch to the hard‑coded remote URL version (recommended)


✅ Re‑run the Action

You will then see:

✅ Successful git push --mirror
✅ Full repo appearing in Azure DevOps
✅ Identical commit SHAs
