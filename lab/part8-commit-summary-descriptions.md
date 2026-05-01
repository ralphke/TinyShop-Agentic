# Part 8: Commit Summary Descriptions and Code Review

In this section you'll use Copilot to generate meaningful commit messages and review your changes before pushing.

## Generate a commit message

1. After making changes in Parts 1–7, open the **Source Control** panel in VS Code (`Ctrl+Shift+G` / `Cmd+Shift+G`).
1. Stage the files you want to commit (click the **+** next to each file, or stage all with the **+** at the top).
1. In the commit message box at the top, click the **sparkle ✨** (Generate Commit Message) button.
1. Review the generated message — it should summarise what changed across all staged files.
1. If you want a different style, update **.github/copilot-instructions.md** (or a prompt file) to include commit message guidance, e.g.:
   ```markdown
   ## Commit messages
   - Summarise the change in one sentence, then list the top 3–5 changes with emoji and short descriptions.
   ```
1. Generate a new message and compare it to the previous one.

## Run a Copilot code review

1. In Copilot Chat, start a new conversation and switch to **Ask** mode.
1. Ask Copilot to review your staged or recent changes:
   ```
   @workspace Review the changes I've made in this session. Are there any potential issues, missing error handling, or improvements I should consider before committing?
   ```
1. Work through any suggestions that make sense, then commit your changes.

## Push and create a pull request

1. Push your branch:
   ```bash
   git push -u origin <your-branch-name>
   ```
1. Open the repository on GitHub and create a pull request. Use Copilot to draft the PR description if the option appears in the PR editor.

**Key Takeaway**: Copilot's commit-message generation and code-review capabilities reduce the cognitive overhead of wrapping up a feature, helping you ship higher-quality code with better documentation.

