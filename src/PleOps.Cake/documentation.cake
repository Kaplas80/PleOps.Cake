#load "setup.cake"

#tool nuget:?package=docfx.console&version=2.59.3
#addin nuget:?package=Cake.DocFx&version=1.0.0
#addin nuget:?package=Cake.Git&version=2.0.0

using System.Linq;
using LibGit2Sharp;

Task("Build-Doc")
    .Description("Build the documentation")
    .Does<BuildInfo>(info =>
{
    if (!FileExists(info.DocFxFile)) {
        Information("There isn't documentation.");
        return;
    }

    string docsDir = System.IO.Path.GetDirectoryName(info.DocFxFile);
    if (FileExists(info.ChangelogFile) && DirectoryExists($"{docsDir}/dev")) {
        CopyFile(info.ChangelogFile, $"{docsDir}/dev/Changelog.md");
    }

    DocFxMetadata(info.DocFxFile);

    var settings = new DocFxBuildSettings {
        OutputPath = info.ArtifactsDirectory,
        WarningsAsErrors = info.WarningsAsErrors,
    };
    DocFxBuild(info.DocFxFile, settings);

    Zip(
        $"{info.ArtifactsDirectory}/_site",
        $"{info.ArtifactsDirectory}/docs.zip");
});

Task("Push-Doc")
    .Description("Push the documentation to GitHub pages")
    .WithCriteria<BuildInfo>((ctxt, info) => info.BuildType != BuildType.Development)
    .Does<BuildInfo>(info =>
{
    // We don't depend on it so it can run as a different stage
    string docBuild = $"{info.ArtifactsDirectory}/docs.zip";
    if (!FileExists(docBuild)) {
        throw new Exception("Documentation is not build. Run 'Build-Doc' task first.");
    }

    // TODO: [#6] Implement multi-version documentation
    string worktreeName = "push-gh-pages";
    string committerName = "PleOps.Cake Bot";
    string committerEmail = "ci@pleops.cake";

    using (Repository repo = new Repository(GitFindRootFromPath(".").FullPath)) {
        bool ownTree = false;
        Worktree tree = null;
        if (repo.Worktrees.Any(w => w.Name == worktreeName)) {
            Information("Re-use worktree");
            tree = repo.Worktrees[worktreeName];
        } else {
            Information("Create worktree");

            // libgit2sharp does not clean the refs and it complains if you run it again later
            string refPath = $"{repo.Info.Path}/refs/heads/{worktreeName}";
            if (FileExists(refPath)) {
                Information("Deleting old ref");
                DeleteFile(refPath);
            }

            CreateDirectory($"{info.ArtifactsDirectory}/tmp");
            tree = repo.Worktrees.Add(
                worktreeName,
                $"{info.ArtifactsDirectory}/tmp/gh-pages",
                false); // no lock since it's not a portable media
            ownTree = true;
        }

        Repository treeRepo = tree.WorktreeRepository;
        string treePath = treeRepo.Info.WorkingDirectory;
        Information($"Worktree at: {treePath}");

        // LibGit2Sharp seems to have a bug where it creates the worktree branch
        // at the current HEAD (main branch), instead at the given reference.
        // So we do the checkout with a pull ourselves.
        var checkoutSettings = new ProcessSettings {
            Arguments = "checkout gh-pages",
            WorkingDirectory = treePath,
        };
        int checkoutResult = StartProcess("git", checkoutSettings);
        if (checkoutResult != 0) {
            Error("Error creating gh-pages branch");
        }

        var pullSettings = new ProcessSettings {
            Arguments = "pull --ff-only origin gh-pages",
            WorkingDirectory = treePath,
        };
        int pullResult = StartProcess("git", pullSettings);
        if (pullResult != 0) {
            Error("Error pulling");
        }

        Information($"Current branch: {treeRepo.Head.FriendlyName}");

        // Clean directory so we don't keep old files
        // Just move temporary the .git file so it's not deleted.
        CopyFile($"{treePath}/.git", $"{info.ArtifactsDirectory}/tmp/.git");
        CleanDirectory(treePath);
        MoveFile($"{info.ArtifactsDirectory}/tmp/.git", $"{treePath}/.git");
        Unzip(docBuild, treePath);

        if (treeRepo.RetrieveStatus().IsDirty) {
            Information("Stage all");
            Commands.Stage(treeRepo, "*");

            Information("Commit");
            treeRepo.Commit(
                $":books: Documentation update for {info.Version}",
                new Signature(committerName, committerEmail, DateTimeOffset.Now),
                new Signature(committerName, committerEmail, DateTimeOffset.Now),
                new CommitOptions());

            // It seems it doesn't support SSH so we run the command manually
            Information("Push");
            var pushSettings = new ProcessSettings {
                Arguments = "push -u origin gh-pages",
                WorkingDirectory = treePath,
            };
            int pushResult = StartProcess("git", pushSettings);
            if (pushResult != 0) {
                Error("Error pushing");
            }
        } else {
            Information("No changes detected, no new commits done");
        }

        if (ownTree) {
            Information("Prune worktree");
            repo.Worktrees.Prune(tree);
            repo.Branches.Remove(worktreeName);
        }
    }
});
