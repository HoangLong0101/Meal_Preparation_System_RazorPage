# GitHub Repository Setup Guide

Your local Git repository has been initialized and your first commit is ready!

## Next Steps to Create Your GitHub Repository

### Method 1: Using GitHub Website (Easiest)

1. **Go to GitHub**: Visit https://github.com/new

2. **Repository Settings**:
   - Repository name: `Meal_Preparation_System_RazorPage`
   - Description: `AI-powered meal preparation and planning system built with ASP.NET Core Razor Pages`
   - Visibility: Choose **Public** or **Private**
   - **IMPORTANT**: DO NOT check these boxes:
     - ? Add a README file
     - ? Add .gitignore
     - ? Choose a license
     (We already have these files!)

3. **Click "Create repository"**

4. **Copy the repository URL** that appears (it will look like):
   ```
   https://github.com/YOUR_USERNAME/Meal_Preparation_System_RazorPage.git
   ```

5. **Run these commands** in your PowerShell terminal:
   ```powershell
   git remote add origin https://github.com/YOUR_USERNAME/Meal_Preparation_System_RazorPage.git
   git push -u origin main
   ```

### Method 2: Using GitHub CLI (If you install it)

Install GitHub CLI from: https://cli.github.com/

Then run:
```powershell
gh auth login
gh repo create Meal_Preparation_System_RazorPage --public --source=. --remote=origin --push
```

## What's Already Done ?

- ? Git repository initialized
- ? All files added to staging
- ? Initial commit created
- ? Branch renamed to 'main'
- ? .gitignore file created (excludes bin, obj, appsettings.Development.json)
- ? README.md created with documentation
- ? LICENSE file created (MIT License)
- ? appsettings.example.json created as a template

## Repository Contents

Your repository includes:

### Frontend (Razor Pages)
- `Meal_Preparation_System_RazorPage/` - Main web application
  - `Pages/Index.cshtml` - Home page
  - `Pages/Recommendations.cshtml` - Meal recommendations page
  - `Pages/MealPlanner.cshtml` - Weekly meal planner
  - `Pages/CustomerProfile.cshtml` - Customer profile lookup
  - `Program.cs` - Application configuration with DI setup

### Documentation
- `README.md` - Comprehensive project documentation
- `LICENSE` - MIT License
- `GITHUB_SETUP.md` - This file

### Configuration
- `.gitignore` - Git ignore rules for .NET projects
- `appsettings.example.json` - Configuration template

## Important Security Notes

### Before Pushing to GitHub:

1. **Never commit these files with real credentials**:
   - `appsettings.Development.json` (already in .gitignore)
   - Any files with real API keys or passwords

2. **The appsettings.json includes only placeholder values**:
   - `YOUR_VNPAY_TMN_CODE`
   - `YOUR_VNPAY_HASH_SECRET`
   - `YOUR_OPENAI_API_KEY_HERE`

3. **Database connection string** uses localdb (safe for public repos)

## Current Git Status

Run `git status` to verify everything is ready:
```powershell
git status
```

You should see:
```
On branch main
nothing to commit, working tree clean
```

## After Creating the Repository

Once you've created the repository on GitHub and pushed your code:

1. **Add repository link to README**:
   - Update YOUR_USERNAME in README.md with your actual GitHub username

2. **Add topics to your repository** (optional):
   - aspnet-core
   - razor-pages
   - dotnet
   - ai-recommendations
   - meal-planning
   - openai

3. **Enable GitHub Pages** (optional):
   - Go to repository Settings Å® Pages
   - Can be used for documentation

## Troubleshooting

### If you get authentication errors when pushing:

**Option A: HTTPS with Personal Access Token**
1. Go to GitHub Å® Settings Å® Developer settings Å® Personal access tokens
2. Generate new token (classic) with 'repo' scope
3. Use the token as your password when pushing

**Option B: SSH**
1. Set up SSH keys: https://docs.github.com/en/authentication/connecting-to-github-with-ssh
2. Change remote URL:
   ```powershell
   git remote set-url origin git@github.com:YOUR_USERNAME/Meal_Preparation_System_RazorPage.git
   ```

## Need Help?

If you encounter any issues, check:
- GitHub documentation: https://docs.github.com/
- Git documentation: https://git-scm.com/doc
- Or open an issue in your repository

---

**Ready to proceed?** Just create the repository on GitHub and run the push command!
