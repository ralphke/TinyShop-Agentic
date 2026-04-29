# Azure DevOps Integration Verification Checklist

Use this guide to verify whether this GitHub repository is connected to Azure DevOps outside the codebase.

## Quick Runbook (2-Minute Check)

Use this when you only need a yes/no answer quickly.

1. In Azure DevOps, open **Pipelines -> Pipelines** and find a pipeline targeting this repo.
2. Open its latest run and copy the commit SHA.
3. In GitHub, open that same commit and verify an **Azure Pipelines** status check is present.
4. In GitHub repository settings, confirm either:
   - A webhook to `dev.azure.com`, or
   - The **Azure Pipelines** GitHub App is installed.
5. Decision:
   - If steps 1-4 all match, integration is active.
   - If no pipeline, no matching check, and no webhook/app, there is likely no active integration.

### Quick Evidence Log Template

- Azure DevOps pipeline:
- Azure DevOps run ID/time:
- Commit SHA:
- GitHub commit or PR URL:
- Azure Pipelines check name:
- Webhook or App proof:
- Final verdict (Integrated / Not integrated):

## 1. Azure DevOps Side

1. Open your Azure DevOps project and go to **Project Settings -> Service connections**.
2. Check for any GitHub or GitHub Enterprise service connection that references this repository.
3. Go to **Pipelines -> Pipelines** and look for pipelines that point to this GitHub repo and branch.
4. Open each candidate pipeline and verify:
   - Repository provider is GitHub.
   - Repository name matches `dotnet-presentations/visual-studio-github-copilot-lab`.
   - YAML path and branch are active.
5. Go to **Project Settings -> Service hooks** and check for subscriptions targeting GitHub events.
6. Go to **Boards -> GitHub connections** and verify whether this repo is linked for work-item integration.
7. Check project and organization settings for any imported or mirrored GitHub repo.

## 2. GitHub Side

1. In GitHub repository settings, open **Webhooks** and check for hooks to `dev.azure.com`.
2. Open **Installed GitHub Apps** and verify whether **Azure Pipelines** is installed for this repository.
3. Open **Branch protection** or **Rulesets** and inspect required status checks:
   - If checks named Azure Pipelines are required, integration is active.
4. Review checks on recent pull requests:
   - Presence of Azure Pipeline jobs confirms external Azure DevOps CI connection.

## 3. End-to-End Flow (Both Sides)

Use this as a fast, practical validation flow.

1. Start in Azure DevOps:
   - Open **Pipelines -> Pipelines**.
   - Find a pipeline that uses this repository.
   - Open the latest run and capture: pipeline name, branch, commit SHA, and run time.
2. Switch to GitHub:
   - Open the matching commit or pull request.
   - Confirm a status check from Azure Pipelines appears and matches the same commit SHA.
3. Return to Azure DevOps:
   - Open the pipeline definition.
   - Confirm repository is `dotnet-presentations/visual-studio-github-copilot-lab` and branch filters include the branch you tested.
4. Confirm trigger path:
   - In GitHub, review **Webhooks** and/or installed **Azure Pipelines app**.
   - In Azure DevOps, verify pipeline trigger settings (CI/PR triggers) are enabled as expected.
5. Confirm auth path:
   - In Azure DevOps **Service connections**, open the GitHub connection and ensure it is authorized and healthy.
6. Perform a live test:
   - Create a tiny non-functional commit on `AgentAPI` (or a test branch).
   - Confirm a new Azure Pipeline run starts.
   - Confirm the same run result appears as a GitHub status check.
7. Record evidence:
   - Pipeline URL, run ID, GitHub commit URL, and screenshot of matching status/check.

## 4. Identity and Token Clues

1. Check secrets and variables in both systems for names such as:
   - `AZDO`
   - `SYSTEM_ACCESSTOKEN`
   - `AZURE_DEVOPS_PAT`
2. In Azure DevOps audit logs, look for pipeline runs and service connection usage tied to this repository.

## 5. What Confirms Integration

Integration is confirmed if one or more of the following are true:

1. An Azure DevOps pipeline is mapped to this GitHub repository.
2. A GitHub webhook or Azure Pipelines GitHub App is configured for this repository.
3. Pull request or commit status checks are posted by Azure Pipelines.
