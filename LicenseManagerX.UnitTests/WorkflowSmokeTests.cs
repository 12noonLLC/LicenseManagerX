using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LicenseManagerX.UnitTests;

[TestClass]
public class WorkflowSmokeTests
{
   [TestMethod]
   public void BuildWorkflow_UsesOneDayArtifactRetention()
   {
      string workflow = ReadBuildWorkflow();

      Assert.Contains("retention-days: 1", workflow);
   }

   [TestMethod]
   public void BuildWorkflow_UsesDotnetTestProjectForm()
   {
      string workflow = ReadBuildWorkflow();

      Assert.Contains("dotnet test", workflow);
      Assert.Contains("--project \"${{ env.PROJECT_TESTS_PATH }}\"", workflow);
   }

   [TestMethod]
   public void BuildWorkflow_DoesNotPublishNuget()
   {
      string workflow = ReadBuildWorkflow();

      Assert.DoesNotContain("dotnet nuget push", workflow);
   }

   [TestMethod]
   public void PublishNugetWorkflow_PublishesNugetWithSkipDuplicate()
   {
      string workflow = ReadPublishNugetWorkflow();

      Assert.Contains("dotnet nuget push", workflow);
      Assert.Contains("--skip-duplicate", workflow);
      Assert.Contains("workflow_dispatch", workflow);
      Assert.Contains("environment: nuget-prod", workflow);
   }

   private static string ReadBuildWorkflow()
   {
      string repoRoot = FindRepoRoot();
      string path = Path.Combine(repoRoot, ".github", "workflows", "build.yml");
      Assert.IsTrue(File.Exists(path), $"Workflow file not found: {path}");
      return File.ReadAllText(path);
   }

   private static string ReadPublishNugetWorkflow()
   {
      string repoRoot = FindRepoRoot();
      string path = Path.Combine(repoRoot, ".github", "workflows", "publish-nuget.yml");
      Assert.IsTrue(File.Exists(path), $"Workflow file not found: {path}");
      return File.ReadAllText(path);
   }

   private static string FindRepoRoot()
   {
      string[] candidates =
      [
         Path.GetFullPath(Path.Combine(GetThisSourceFileDirectory(), "..")),
         Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "..", "..")),
      ];

      foreach (string candidate in candidates)
      {
         if (File.Exists(Path.Combine(candidate, "Directory.Packages.props")))
         {
            return candidate;
         }
      }

      throw new DirectoryNotFoundException("Could not locate repository root for workflow smoke tests.");
   }

   private static string GetThisSourceFileDirectory()
   {
      string? sourcePath = GetThisSourceFilePath();
      if (string.IsNullOrWhiteSpace(sourcePath))
      {
         throw new InvalidOperationException("Could not determine source file path.");
      }

      string? sourceDir = Path.GetDirectoryName(sourcePath);
      if (string.IsNullOrWhiteSpace(sourceDir))
      {
         throw new InvalidOperationException("Could not determine source file directory.");
      }

      return sourceDir;
   }

   private static string GetThisSourceFilePath([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
