# Security Policy

## Supported Versions

This project is actively maintained only on the latest stable release branch.

| Version | Supported          |
| ------- | ------------------ |
| main    | :white_check_mark: |
| latest release | :white_check_mark: |
| legacy branches | :x: |
| archived versions | :x: |

Security fixes are only guaranteed for the `main` branch and the most recent tagged release.

---

## Security Scope

This repository contains:

- **C# / .NET 10** application code
- **T-SQL** database objects, scripts, and migrations
- **Python 3.14** services, scripts, and automation
- **Container definitions** and runtime configuration
- **Aspire orchestration assets**
- CI/CD pipelines and deployment configuration

Security controls apply to all source, infrastructure, and configuration assets in this repository.

---

## Secure Development Requirements

### Secrets Management

The following must **never** be committed to the repository:

- API keys
- database passwords
- access tokens
- certificates or private keys
- connection strings containing credentials
- `.env` files with secrets
- cloud credentials
- service principal secrets

Use instead:

- environment variables
- GitHub Actions secrets
- Azure Key Vault, AWS Secrets Manager, or equivalent secret stores
- local secret managers such as `.NET user-secrets`

Recommended ignored files:

```gitignore
.env
.env.*
secrets.json
*.pfx
*.key
*.pem
appsettings.Development.json
````

---

### C# / .NET Security

For C# code:

* Target **.NET 10** only
* Enable nullable reference types
* Treat warnings as errors where practical
* Enable analyzers and security rules
* Validate all external input
* Avoid unsafe deserialization
* Avoid reflection-based loading from untrusted sources
* Use parameterized queries only

Recommended protections:

* `Microsoft.CodeAnalysis.NetAnalyzers`
* `dotnet list package --vulnerable`
* `dotnet restore --locked-mode`

Do not:

* hardcode credentials
* disable TLS validation
* log secrets or tokens

---

### T-SQL Security

All SQL code must:

* use parameterized statements
* avoid dynamic SQL unless strictly necessary
* sanitize object names if dynamic SQL is unavoidable
* apply least-privilege permissions
* avoid granting `sysadmin`, `db_owner`, or broad rights unnecessarily

Required:

* schema-qualified object references
* explicit transactions where appropriate
* migration review before execution

Forbidden:

* inline credentials
* production connection strings in scripts
* ad-hoc privilege escalation scripts

---

### Python Security

Python components must:

* target **Python 3.14**
* pin dependencies where practical
* validate external input
* avoid unsafe `eval`, `exec`, and deserialization

Required dependency scanning:

```bash
pip audit
```

Recommended:

```bash
python -m venv .venv
pip install --upgrade pip
```

Do not:

* commit virtual environments
* store secrets in source files
* execute untrusted code dynamically

---

### Container Security

Containers must be hardened.

Requirements:

* use minimal base images
* pin image versions where possible
* run as non-root
* define explicit users
* minimize installed packages
* scan images for vulnerabilities

Recommended scanning:

```bash
docker scout quickview
trivy image <image>
```

Container rules:

* no embedded secrets
* no plaintext credentials
* avoid `latest` in production releases
* expose only required ports

Example:

```dockerfile
USER app
```

---

### Configuration Security

Configuration files must not expose:

* secrets
* internal certificates
* production endpoints with credentials

Protected files include:

* `appsettings*.json`
* Aspire manifests
* YAML pipeline files
* deployment manifests
* compose files
* Kubernetes manifests

Rules:

* separate environment-specific configuration
* prefer secret injection at runtime

---

### Dependency and Supply Chain Security

All dependencies must be monitored.

Required:

* Dependabot or equivalent enabled
* lock files committed where applicable
* vulnerability scanning in CI

Recommended checks:

```bash
dotnet list package --vulnerable
pip audit
docker scout
```

Third-party packages should be:

* actively maintained
* version pinned or constrained
* reviewed before upgrade

---

## Reporting a Vulnerability

Please report vulnerabilities responsibly.

### Report Method

Open a **private security advisory** or contact maintainers directly.

Do **not** open public GitHub issues for security vulnerabilities.

Include:

* vulnerability description
* affected component(s)
* reproduction steps
* proof of concept (if available)
* impact assessment
* suggested remediation (optional)

---

## Response Timeline

Expected response targets:

| Stage                    | Target              |
| ------------------------ | ------------------- |
| Initial acknowledgment   | 72 hours            |
| Triage decision          | 7 business days     |
| Status update            | every 14 days       |
| Fix or mitigation target | depends on severity |

Severity priorities:

* Critical: immediate triage
* High: expedited remediation
* Medium/Low: scheduled remediation

---

## Disclosure Policy

After validation:

* issue is triaged
* fix is prepared
* patched release is published
* coordinated disclosure may occur

If a report is declined, maintainers will provide rationale where possible.

---

## Security Automation

This repository should enable:

* GitHub Dependabot
* GitHub CodeQL
* secret scanning
* container scanning
* branch protection rules
* signed commits/tags where feasible

Recommended branch protections:

* required PR reviews
* status checks required
* blocked force pushes
* restricted direct pushes to `main`

---

## Supported Runtime Baselines

Current supported platform baselines:

* Aspire: `latest`
* .NET SDK: `10`
* Python: `3.14`

Updates to runtime baselines may require security-related breaking changes.

````

A couple of practical notes: using `aspire:latest` and other `latest` tags is convenient for development, but from a security and reproducibility standpoint it’s not ideal for production. Prefer pinned digests or explicit versions in release branches, otherwise you can’t reliably reproduce builds or know exactly what changed.

Put this file at:

```bash
.github/SECURITY.md
````
