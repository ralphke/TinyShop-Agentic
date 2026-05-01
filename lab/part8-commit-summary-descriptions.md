# Part 8: Commit Summary Descriptions and Code Review

In this section you'll use Copilot to generate meaningful commit messages and review your changes before pushing.

## Generate a commit message

| IDE | Steps |
|:----|:------|
| **VS Code / Codespaces** | Open the **Source Control** panel (`Ctrl+Shift+G` / `Cmd+Shift+G`) → stage your files → click the **sparkle ✨** (Generate Commit Message) button in the message box |
| **Visual Studio 2026** | Open **View → Git Changes** → stage your files → click the **sparkle ✨ pencil** button above the commit message box |

Review the generated message — it should summarise what changed across all staged files.

## Customise the commit message style

| IDE | How to customise |
|:----|:----------------|
| **VS Code / Codespaces** | Add a `## Commit messages` section to **.github/copilot-instructions.md** with your preferred format |
| **Visual Studio 2026** | Go to **Tools → Options → GitHub → Copilot → Source Control Integration** and update the commit message prompt field |

Example customisation:
```markdown
## Commit messages
- Summarise the change in one sentence, then list the top 3–5 changes with emoji and short descriptions.
```

Generate a new commit message after the change and compare it to the previous one.

## Run a Copilot code review

| IDE | Steps |
|:----|:------|
| **VS Code / Codespaces** | In Copilot Chat, switch to **Ask** mode and ask: `@workspace Review the changes I've made in this session. Are there any potential issues, missing error handling, or improvements I should consider before committing?` |
| **Visual Studio 2026** | Open **View → Git Changes** → enable the **Code Review Assistance** toggle, then stage your files; Copilot will add inline review comments |

Work through any suggestions that make sense, then commit your changes.

## Push and create a pull request

1. Push your branch:
   ```bash
   git push -u origin <your-branch-name>
   ```
1. Open the repository on GitHub and create a pull request. Use Copilot to draft the PR description if the option appears in the PR editor.

**Key Takeaway**: Copilot's commit-message generation and code-review capabilities reduce the cognitive overhead of wrapping up a feature, helping you ship higher-quality code with better documentation.


