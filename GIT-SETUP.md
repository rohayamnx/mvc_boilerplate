# Git Setup & Push Guide

This guide walks you through pushing the `mvc_boilerplate` project to GitHub.

---

## Prerequisites

1. **Git installed** — verify with:
   ```powershell
   git --version
   ```

2. **GitHub account** — create one at [github.com](https://github.com) if you don't have one.

3. **Git identity configured** (only needed once per machine):
   ```powershell
   git config --global user.name "Your Name"
   git config --global user.email "your.email@example.com"
   ```

---

## Step 1 — Initialize the Repository

```powershell
git init
```

This creates a new `.git` folder in the project root and sets up the default branch (`main`).

---

## Step 2 — Verify `.gitignore` is Working

Check that sensitive files are excluded:
```powershell
git status
```

The following should **NOT** appear in the untracked files list:
- `bin/`, `obj/`
- `appsettings.Development.json`
- `node_modules/`
- `*.user` files

---

## Step 3 — Stage All Files

```powershell
git add .
```

To verify what's staged before committing:
```powershell
git status
```

---

## Step 4 — Create Your First Commit

```powershell
git commit -m "Initial commit"
```

---

## Step 5 — Create a Repository on GitHub

1. Go to [github.com/new](https://github.com/new)
2. Enter a repository name (e.g. `mvc_boilerplate`)
3. Set visibility:
   - **Public** — anyone can see it
   - **Private** — only you and collaborators
4. **Do NOT** check:
   - "Add a README file"
   - "Add .gitignore"
   - "Choose a license"
   > You already have a `.gitignore` and related files locally.
5. Click **Create repository**

---

## Step 6 — Link the Remote and Push

### Option A — HTTPS (easiest for beginners)

Copy the URL from GitHub (looks like `https://github.com/YOUR-USERNAME/mvc_boilerplate.git`):

```powershell
git remote add origin https://github.com/YOUR-USERNAME/mvc_boilerplate.git
git branch -M main
git push -u origin main
```

You'll be prompted to authenticate with your GitHub username and a **Personal Access Token** (not your password).

> **Creating a Personal Access Token:**
> 1. Go to [github.com/settings/tokens](https://github.com/settings/tokens)
> 2. Click **Generate new token (classic)**
> 3. Give it a name, set an expiration
> 4. Check the `repo` scope
> 5. Click **Generate token**
> 6. Copy the token — use it as your password when pushing

### Option B — SSH (recommended for frequent use)

First, generate an SSH key if you don't have one:
```powershell
ssh-keygen -t ed25519 -C "your.email@example.com"
```

Add the public key to GitHub:
1. Open `~/.ssh/id_ed25519.pub` and copy its contents
2. Go to [github.com/settings/keys](https://github.com/settings/keys)
3. Click **New SSH key**, paste, and save

Then link and push:
```powershell
git remote add origin git@github.com:YOUR-USERNAME/mvc_boilerplate.git
git branch -M main
git push -u origin main
```

---

## Step 7 — Verify the Push

1. Open your repository on GitHub
2. Confirm all files and folders are visible
3. Confirm `appsettings.Development.json` is **not** present

---

## Quick Reference — Common Commands

| Action | Command |
|---|---|
| Check status | `git status` |
| Stage all changes | `git add .` |
| Commit changes | `git commit -m "your message"` |
| Push to GitHub | `git push` |
| Pull latest | `git pull` |
| View remote URL | `git remote -v` |
| Change remote URL | `git remote set-url origin NEW_URL` |
| Remove a file from tracking (keep local) | `git rm --cached filename` |
| Remove remote link | `git remote remove origin` |
| Delete local git repo entirely | `Remove-Item -Recurse -Force .git` |

---

## Troubleshooting

### "fatal: remote origin already exists"
```powershell
git remote set-url origin https://github.com/YOUR-USERNAME/mvc_boilerplate.git
```

### "Updates were rejected because the remote contains work"
```powershell
git pull origin main --allow-unrelated-histories
git push -u origin main
```

### Files that should be ignored are still tracked
If `appsettings.Development.json` was committed before being added to `.gitignore`:
```powershell
git rm --cached appsettings.Development.json
git commit -m "Remove appsettings.Development.json from tracking"
git push
```

### Authentication fails over HTTPS
Use a **Personal Access Token** instead of your password, or switch to SSH:
```powershell
git remote set-url origin git@github.com:YOUR-USERNAME/mvc_boilerplate.git
```

---

## Removing / Resetting Git

If you need to unlink or completely remove git from the project (e.g. you deleted the remote GitHub repo and want to start fresh):

### Remove the remote link only (keeps local history)
```powershell
git remote remove origin
```
Your local commits remain intact. Re-link a new remote anytime with `git remote add origin URL`.

### Completely remove the local git repository (wipes all history)
```powershell
Remove-Item -Recurse -Force .git
```
