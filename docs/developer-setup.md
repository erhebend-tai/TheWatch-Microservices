# TheWatch Developer Setup Guide

> **Item 337 - Signed Git Commits and Repository Protection**
>
> Compliance: SSDF PS.1 (Protect All Forms of Code from Unauthorized Access and Tampering),
> NIST SP 800-53 CM-3 (Configuration Change Control)

This guide walks you through setting up your development environment for TheWatch.
Every commit to this repository **must be cryptographically signed**. This is enforced
by branch protection rules and is a non-negotiable security requirement for an emergency
response platform.

You have two options: **GPG signing** (traditional, widely supported) or **SSH signing**
(newer, simpler setup). Choose one.

---

## Table of Contents

1. [Option A: GPG Commit Signing](#option-a-gpg-commit-signing)
2. [Option B: SSH Commit Signing](#option-b-ssh-commit-signing)
3. [Adding Your Key to GitHub](#adding-your-key-to-github)
4. [Verifying Your Setup](#verifying-your-setup)
5. [Branch Protection Rules](#branch-protection-rules)
6. [Troubleshooting](#troubleshooting)
7. [Compliance References](#compliance-references)

---

## Option A: GPG Commit Signing

GPG signing uses a public/private keypair to prove that commits were authored by you.
This is the traditional method and works with all Git hosting providers.

### Windows

1. **Install GPG (via Git for Windows or Gpg4win)**

   Git for Windows ships with GPG. Verify it is available:

   ```powershell
   gpg --version
   ```

   If not found, install [Gpg4win](https://www.gpg4win.org/). During installation,
   select the "GnuPG" component at minimum.

2. **Generate a GPG key**

   ```powershell
   gpg --full-generate-key
   ```

   When prompted:
   - Key type: select **(1) RSA and RSA**
   - Key size: **4096** (required by GitHub)
   - Expiration: **1y** (one year -- you can extend later)
   - Real name: Use your full name as it appears on your GitHub account
   - Email: Use the email associated with your GitHub account
   - Passphrase: Choose a strong passphrase and store it in your password manager

3. **Find your GPG key ID**

   ```powershell
   gpg --list-secret-keys --keyid-format=long
   ```

   Output will look like:

   ```
   sec   rsa4096/3AA5C34371567BD2 2024-01-15 [SC] [expires: 2025-01-15]
         A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2
   uid                 [ultimate] Your Name <your.email@example.com>
   ssb   rsa4096/42B317FD4BA89E7A 2024-01-15 [E] [expires: 2025-01-15]
   ```

   Your key ID is the part after `rsa4096/` on the `sec` line. In this example:
   `3AA5C34371567BD2`.

4. **Configure Git to use your GPG key**

   ```powershell
   git config --global user.signingkey 3AA5C34371567BD2
   git config --global commit.gpgsign true
   git config --global tag.gpgsign true
   ```

5. **Point Git to the correct GPG binary** (Windows-specific)

   If Git cannot find GPG, explicitly set the path:

   ```powershell
   # If using Git for Windows bundled GPG:
   git config --global gpg.program "C:/Program Files/Git/usr/bin/gpg.exe"

   # If using Gpg4win:
   git config --global gpg.program "C:/Program Files (x86)/GnuPG/bin/gpg.exe"
   ```

6. **Export your public key** (you will add this to GitHub in a later step)

   ```powershell
   gpg --armor --export 3AA5C34371567BD2
   ```

   Copy the entire output including the `-----BEGIN PGP PUBLIC KEY BLOCK-----` and
   `-----END PGP PUBLIC KEY BLOCK-----` lines.

### macOS

1. **Install GPG**

   ```bash
   brew install gnupg pinentry-mac
   ```

2. **Generate a GPG key**

   ```bash
   gpg --full-generate-key
   ```

   Follow the same prompts as Windows above (RSA 4096, 1 year expiry).

3. **Find your key ID**

   ```bash
   gpg --list-secret-keys --keyid-format=long
   ```

4. **Configure Git**

   ```bash
   git config --global user.signingkey 3AA5C34371567BD2
   git config --global commit.gpgsign true
   git config --global tag.gpgsign true
   ```

5. **Configure the GPG agent for passphrase caching**

   Add to `~/.gnupg/gpg-agent.conf`:

   ```
   pinentry-program /opt/homebrew/bin/pinentry-mac
   default-cache-ttl 3600
   max-cache-ttl 86400
   ```

   Then reload the agent:

   ```bash
   gpgconf --kill gpg-agent
   ```

6. **Add GPG_TTY to your shell profile** (`~/.zshrc` or `~/.bashrc`):

   ```bash
   export GPG_TTY=$(tty)
   ```

7. **Export your public key**

   ```bash
   gpg --armor --export 3AA5C34371567BD2
   ```

### Linux

1. **Install GPG**

   ```bash
   # Debian/Ubuntu
   sudo apt-get install gnupg2

   # Fedora/RHEL
   sudo dnf install gnupg2

   # Arch
   sudo pacman -S gnupg
   ```

2. **Generate a GPG key**

   ```bash
   gpg --full-generate-key
   ```

   Follow the same prompts as above (RSA 4096, 1 year expiry).

3. **Find your key ID**

   ```bash
   gpg --list-secret-keys --keyid-format=long
   ```

4. **Configure Git**

   ```bash
   git config --global user.signingkey 3AA5C34371567BD2
   git config --global commit.gpgsign true
   git config --global tag.gpgsign true
   ```

5. **Add GPG_TTY to your shell profile** (`~/.bashrc`):

   ```bash
   export GPG_TTY=$(tty)
   ```

6. **Export your public key**

   ```bash
   gpg --armor --export 3AA5C34371567BD2
   ```

---

## Option B: SSH Commit Signing

SSH signing is simpler to set up because you likely already have an SSH key for
authenticating with GitHub. Git 2.34+ supports using SSH keys for commit signing.

> **Prerequisite**: Git 2.34 or later. Check with `git --version`.

### All Platforms (Windows, macOS, Linux)

1. **Generate an SSH key** (skip if you already have one for GitHub)

   ```bash
   ssh-keygen -t ed25519 -C "your.email@example.com"
   ```

   When prompted for a file location, accept the default (`~/.ssh/id_ed25519`).
   Set a passphrase.

2. **Configure Git to use SSH signing**

   ```bash
   git config --global gpg.format ssh
   git config --global user.signingkey ~/.ssh/id_ed25519.pub
   git config --global commit.gpgsign true
   git config --global tag.gpgsign true
   ```

   On Windows, use the full path:

   ```powershell
   git config --global user.signingkey "C:/Users/YOUR_USERNAME/.ssh/id_ed25519.pub"
   ```

3. **Set up an allowed signers file** (for local verification)

   Create the file `~/.ssh/allowed_signers`:

   ```
   your.email@example.com ssh-ed25519 AAAA...your_public_key_here
   ```

   Tell Git about it:

   ```bash
   git config --global gpg.ssh.allowedSignersFile ~/.ssh/allowed_signers
   ```

   This lets you verify signed commits locally with `git log --show-signature`.

---

## Adding Your Key to GitHub

Both GPG and SSH signing keys must be registered with your GitHub account.

### For GPG Keys

1. Go to [GitHub Settings > SSH and GPG Keys](https://github.com/settings/keys)
2. Click **New GPG key**
3. Paste the full public key output (from the `gpg --armor --export` command)
4. Click **Add GPG key**

### For SSH Keys (Signing)

1. Go to [GitHub Settings > SSH and GPG Keys](https://github.com/settings/keys)
2. Click **New SSH key**
3. Set **Key type** to **Signing Key** (not Authentication Key)
4. Paste the contents of your `~/.ssh/id_ed25519.pub` file
5. Click **Add SSH key**

> **Important**: If you use the same SSH key for both authentication and signing,
> you must add it twice -- once as an "Authentication Key" and once as a "Signing Key".

---

## Verifying Your Setup

After configuration, verify that signing works:

```bash
# Create a test commit
echo "test" > /tmp/signing-test.txt
cd /tmp && git init signing-test && cd signing-test
git add . && git commit -m "Test signed commit"

# Verify the signature
git log --show-signature -1
```

You should see `Good signature` or `Good "git" signature` in the output.

For TheWatch specifically, clone the repo and make a test commit on a feature branch:

```bash
git clone git@github.com:your-org/TheWatch.git
cd TheWatch
git checkout -b test/verify-signing
echo "# Signing test" > test-signing.md
git add test-signing.md
git commit -m "test: verify commit signing setup"
git log --show-signature -1
```

Delete the test branch when done. Do not push it.

---

## Branch Protection Rules

TheWatch enforces the following branch protection rules on `main`. These rules exist
to satisfy SSDF PS.1 and NIST CM-3 requirements for configuration change control on
a life-safety platform.

Repository administrators must configure these in
**Settings > Branches > Branch protection rules** for the `main` branch:

### Required Rules

| Rule | Setting | Rationale |
|------|---------|-----------|
| **Require signed commits** | Enabled | Every commit on main must have a verified GPG or SSH signature. Prevents impersonation and ensures non-repudiation. (SSDF PS.1, NIST CM-3) |
| **Require pull request reviews** | 2 approvals minimum | No direct pushes to main. All changes must be reviewed by at least two team members who did not author the change. |
| **Dismiss stale reviews** | Enabled | If new commits are pushed to a PR after approval, previous approvals are dismissed. Reviewers must re-approve the updated code. |
| **Require review from code owners** | Enabled | Changes to security-critical paths (auth, crypto, infrastructure) require approval from designated code owners. |
| **Require status checks to pass** | Enabled | The following checks must pass before merge: |
| | `ci` | Build and unit test pipeline |
| | `security` | SAST, dependency scanning, secret detection |
| | `docker-publish` | Container image build and push |
| **Require branches to be up to date** | Enabled | PR branch must be up to date with main before merging. Prevents merge skew. |
| **Require linear history** | Enabled | Only squash merges or rebase merges are allowed. No merge commits. Produces a clean, auditable history. |
| **No force pushes** | Enabled (locked) | Force pushing to main is prohibited. History must never be rewritten on the default branch. |
| **No deletions** | Enabled | The main branch cannot be deleted. |
| **Restrict who can push** | Team leads + CI bot only | Only designated team leads and the CI service account can merge to main. |

### CODEOWNERS Configuration

Create a `CODEOWNERS` file in the repository root to enforce review ownership:

```
# TheWatch CODEOWNERS
# These owners are automatically requested for review when their paths are modified.

# Security-critical: authentication, authorization, cryptography
/TheWatch.P5.AuthSecurity/          @thewatch/security-team
/TheWatch.Contracts.AuthSecurity/   @thewatch/security-team

# Infrastructure: CI/CD, Docker, Terraform, Helm
/.github/                           @thewatch/platform-team @thewatch/security-team
/docker/                            @thewatch/platform-team
/terraform/                         @thewatch/platform-team
/helm/                              @thewatch/platform-team
/docker-compose*.yml                @thewatch/platform-team

# Core gateway: routing, rate limiting, API surface
/TheWatch.P1.CoreGateway/           @thewatch/core-team
/TheWatch.Contracts.CoreGateway/    @thewatch/core-team

# Emergency services: voice, first responder, disaster relief
/TheWatch.P2.VoiceEmergency/        @thewatch/emergency-team @thewatch/core-team
/TheWatch.P6.FirstResponder/        @thewatch/emergency-team
/TheWatch.P8.DisasterRelief/        @thewatch/emergency-team

# Mesh networking: critical for offline/degraded operation
/TheWatch.P3.MeshNetwork/           @thewatch/core-team @thewatch/emergency-team

# All contract changes require core team review
/TheWatch.Contracts.*/              @thewatch/core-team

# Shared libraries affect all services
/TheWatch.Shared/                   @thewatch/core-team
/Directory.Build.props              @thewatch/platform-team
/Directory.Packages.props           @thewatch/platform-team
```

### Ruleset Configuration (GitHub Rulesets - recommended)

For organizations using GitHub Rulesets (newer, more flexible than branch protection):

```json
{
  "name": "TheWatch Main Branch Protection",
  "target": "branch",
  "enforcement": "active",
  "conditions": {
    "ref_name": {
      "include": ["refs/heads/main"],
      "exclude": []
    }
  },
  "rules": [
    { "type": "deletion" },
    { "type": "non_fast_forward" },
    { "type": "required_signatures" },
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": 2,
        "dismiss_stale_reviews_on_push": true,
        "require_code_owner_review": true,
        "require_last_push_approval": true,
        "required_review_thread_resolution": true
      }
    },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": true,
        "required_status_checks": [
          { "context": "ci" },
          { "context": "security" },
          { "context": "docker-publish" }
        ]
      }
    },
    { "type": "required_linear_history" }
  ]
}
```

---

## Troubleshooting

### "error: gpg failed to sign the data"

**Windows**: Git may not find the GPG binary. Set the path explicitly:

```powershell
git config --global gpg.program "C:/Program Files/Git/usr/bin/gpg.exe"
```

**macOS/Linux**: The GPG agent cannot prompt for your passphrase. Ensure `GPG_TTY`
is set:

```bash
export GPG_TTY=$(tty)
```

If using a GUI IDE (VS Code, Rider), the agent may need `pinentry-mac` or
`pinentry-gnome3` configured.

### Commits show "Unverified" on GitHub

- Confirm the email in your GPG/SSH key matches your GitHub email
- Confirm the key is uploaded to GitHub (Settings > SSH and GPG Keys)
- For GPG: ensure the key has not expired (`gpg --list-keys`)
- For SSH: ensure the key was added as a **Signing Key**, not just an Authentication Key

### "error: Load key ... Permission denied"

SSH key permissions are too open. Fix:

```bash
chmod 600 ~/.ssh/id_ed25519
chmod 644 ~/.ssh/id_ed25519.pub
```

### GPG key expired

Extend the expiry without generating a new key:

```bash
gpg --edit-key 3AA5C34371567BD2
gpg> expire
# Follow prompts to set new expiry
gpg> save
```

Then re-upload the updated public key to GitHub.

### VS Code integration

Add to your VS Code `settings.json`:

```json
{
  "git.enableCommitSigning": true
}
```

For GPG on Windows, you may also need:

```json
{
  "git.path": "C:\\Program Files\\Git\\bin\\git.exe"
}
```

### JetBrains Rider integration

Go to **Settings > Version Control > Git** and check **Sign commits using GPG**.
Set the GPG executable path if it is not auto-detected.

---

## Compliance References

This setup guide addresses the following compliance requirements:

### SSDF PS.1 - Protect All Forms of Code from Unauthorized Access and Tampering

- **PS.1.1**: Store all code in a version control system (Git) with access controls
- **PS.1.2**: Require multi-factor authentication for repository access
- **PS.1.3**: Enforce signed commits to establish non-repudiation and verify author identity
- **PS.1.4**: Use branch protection rules to prevent unauthorized changes to production code
- **PS.1.5**: Maintain audit trail of all code changes (linear history, signed commits)

Signed commits ensure that every change to TheWatch's codebase can be attributed to
a verified developer identity. Combined with branch protection rules requiring pull
request reviews, this creates a change control process where code modifications are
reviewed, approved, and cryptographically attributed before reaching the main branch.

### NIST SP 800-53 CM-3 - Configuration Change Control

- **CM-3(a)**: Determine and document types of changes under configuration control
  (all source code, infrastructure-as-code, CI/CD pipelines)
- **CM-3(b)**: Review proposed changes and approve/disapprove with explicit consideration
  of security impact (pull request reviews with 2 approvals)
- **CM-3(c)**: Document change decisions (PR descriptions, review comments, commit messages)
- **CM-3(d)**: Implement approved changes (merge to main after all checks pass)
- **CM-3(e)**: Retain records of changes (Git log with signed commits, linear history)
- **CM-3(f)**: Monitor and review activities associated with changes (required status
  checks: ci, security, docker-publish)
- **CM-3(g)**: Coordinate change control with organizational elements (CODEOWNERS,
  team-based review requirements)

For an emergency response platform like TheWatch, configuration change control is
critical. Unauthorized or unreviewed code changes to services like VoiceEmergency
or FirstResponder could directly impact life-safety operations. The controls in this
guide ensure that every change is intentional, reviewed, and traceable.

### Related Controls

| Control | Description | How We Address It |
|---------|-------------|-------------------|
| SSDF PS.2 | Protect build integrity | See Item 338 (SLSA provenance workflow) |
| NIST CM-5 | Access restrictions for change | Branch protection, CODEOWNERS |
| NIST CM-6 | Configuration settings | `Directory.Build.props`, `nuget.config` tracked in Git |
| NIST AU-10 | Non-repudiation | Signed commits with verified GPG/SSH keys |
| NIST IA-7 | Cryptographic module authentication | GPG 4096-bit RSA or Ed25519 SSH keys |
| OWASP A08 | Software and data integrity failures | Signed commits + SLSA provenance |
