﻿using System.Linq;
using Kudu.Core.Deployment;
using Kudu.FunctionalTests.Infrastructure;
using Kudu.TestHarness;
using Xunit;

namespace Kudu.FunctionalTests
{
    public class SelectNodeVersionFacts
    {
        [Fact]
        public void TestFallbackToDefaultNodeJsVersionWithServerJsOnly()
        {
            // Arrange
            string appName = KuduUtils.GetRandomWebsiteName("FallbackToDefaultNodeJsVersionWithServerJsOnly");

            using (var repo = Git.Clone("VersionPinnedNodeJsApp"))
            {
                repo.DeleteFile("iisnode.yml");
                repo.DeleteFile("package.json");

                Git.Commit(repo.PhysicalPath, "Changes");

                ApplicationManager.Run(appName, appManager =>
                {
                    // Act
                    GitDeploymentResult deployResult = appManager.GitDeploy(repo.PhysicalPath);
                    var results = appManager.DeploymentManager.GetResultsAsync().Result.ToList();

                    // Assert
                    Assert.Equal(1, results.Count);
                    Assert.Equal(DeployStatus.Success, results[0].Status);
                    Assert.Contains("The package.json file is not present", deployResult.GitTrace);
                });
            }
        }

        [Fact]
        public void TestFallbackToDefaultNodeJsVersionWithServerJsAndEmptyPackageJson()
        {
            // Arrange
            string appName = KuduUtils.GetRandomWebsiteName("FallbackToDefaultNodeJsVersionWithServerJsAndEmptyPackageJson");

            using (var repo = Git.Clone("VersionPinnedNodeJsApp"))
            {
                repo.DeleteFile("iisnode.yml");
                repo.WriteFile("package.json", @"{ ""name"" : ""foo"", ""version"": ""1.0.0"" }");

                Git.Commit(repo.PhysicalPath, "Changes");

                ApplicationManager.Run(appName, appManager =>
                {
                    // Act
                    GitDeploymentResult deployResult = appManager.GitDeploy(repo.PhysicalPath);
                    var results = appManager.DeploymentManager.GetResultsAsync().Result.ToList();

                    // Assert
                    Assert.Equal(1, results.Count);
                    Assert.Equal(DeployStatus.Success, results[0].Status);
                    Assert.Contains("The package.json file does not specify node.js engine version constraints", deployResult.GitTrace);
                });
            }

        }

        [Fact]
        public void TestFallbackToDefaultNodeJsVersionWithIisnodeYmlLackingNodeProcessCommandLine()
        {
            // Arrange
            string appName = KuduUtils.GetRandomWebsiteName("FallbackToDefaultNodeJsVersionWithIisnodeYmlLacking");

            using (var repo = Git.Clone("VersionPinnedNodeJsApp"))
            {
                repo.WriteFile("iisnode.yml", "foo: bar");
                repo.DeleteFile("package.json");
                Git.Commit(repo.PhysicalPath, "Changes");

                ApplicationManager.Run(appName, appManager =>
                {
                    // Act
                    GitDeploymentResult deployResult = appManager.GitDeploy(repo.PhysicalPath);
                    var results = appManager.DeploymentManager.GetResultsAsync().Result.ToList();

                    // Assert
                    Assert.Equal(1, results.Count);
                    Assert.Equal(DeployStatus.Success, results[0].Status);
                    Assert.Contains("The package.json file is not present", deployResult.GitTrace);
                });
            }
        }

        [Fact]
        public void TestTurningAutomaticVersionSelectionOffWithIisnodeYmlWithNodeProcessCommandLine()
        {
            // Arrange
            string appName = KuduUtils.GetRandomWebsiteName("TurningAutomaticVersionSelectionOffWithIisnodeYml");

            using (var repo = Git.Clone("VersionPinnedNodeJsApp"))
            {
                repo.WriteFile("iisnode.yml", "nodeProcessCommandLine: bar");
                Git.Commit(repo.PhysicalPath, "Changes");

                ApplicationManager.Run(appName, appManager =>
                {
                    // Act
                    GitDeploymentResult deployResult = appManager.GitDeploy(repo.PhysicalPath);
                    var results = appManager.DeploymentManager.GetResultsAsync().Result.ToList();

                    // Assert
                    Assert.Equal(1, results.Count);
                    Assert.Equal(DeployStatus.Success, results[0].Status);
                    Assert.Contains("The iisnode.yml file explicitly sets nodeProcessCommandLine", deployResult.GitTrace);
                });
            }
        }

        [Fact]
        public void TestMismatchBetweenAvailableVersionsAndRequestedVersions()
        {
            // Arrange
            string appName = KuduUtils.GetRandomWebsiteName("TestMismatchBetweenAvailableVersionsAndRequestedVersions");

            using (var repo = Git.Clone("VersionPinnedNodeJsApp"))
            {
                repo.Replace("package.json", "0.8.2", "0.1.0");
                Git.Commit(repo.PhysicalPath, "Changes");

                ApplicationManager.Run(appName, appManager =>
                {
                    // Act
                    GitDeploymentResult deployResult = appManager.GitDeploy(repo.PhysicalPath);
                    var results = appManager.DeploymentManager.GetResultsAsync().Result.ToList();

                    // Assert
                    Assert.Equal(1, results.Count);
                    Assert.Equal(DeployStatus.Failed, results[0].Status);
                    Assert.Contains("No available node.js version matches application's version constraint of '0.1.0'", deployResult.GitTrace);
                });
            }
        }

        [Fact]
        public void TestPositiveMatch()
        {
            // Arrange
            string appName = KuduUtils.GetRandomWebsiteName("VersionPinnedNodeJsApp");

            using (var repo = Git.Clone("VersionPinnedNodeJsApp"))
            {
                ApplicationManager.Run(appName, appManager =>
                {
                    // Act
                    GitDeploymentResult deployResult = appManager.GitDeploy(repo.PhysicalPath);
                    var results = appManager.DeploymentManager.GetResultsAsync().Result.ToList();

                    // Assert
                    Assert.Equal(1, results.Count);
                    Assert.Equal(DeployStatus.Success, results[0].Status);
                    KuduAssert.VerifyUrl(appManager.SiteUrl, "v0.8.2");
                });
            }
        }

        [Fact]
        public void SelectNodeVersionShouldWorkWithCustomDeployment()
        {
            // Arrange
            string repositoryName = "VersionPinnedNodeJsApp";
            string appName = KuduUtils.GetRandomWebsiteName("VersionPinnedNodeJsApp");
            string cloneUrl = "https://github.com/KuduApps/VersionPinnedNodeJsAppCustom.git";

            using (var repo = Git.Clone(repositoryName, cloneUrl))
            {
                ApplicationManager.Run(appName, appManager =>
                {
                    // Act
                    GitDeploymentResult deployResult = appManager.GitDeploy(repo.PhysicalPath);
                    var results = appManager.DeploymentManager.GetResultsAsync().Result.ToList();

                    // Assert
                    Assert.Equal(1, results.Count);
                    Assert.Equal(DeployStatus.Success, results[0].Status);
                    KuduAssert.VerifyUrl(appManager.SiteUrl, "v0.8.2");
                    KuduAssert.VerifyLogOutput(appManager, results[0].Id, "custom deployment success");
                });
            }
        }
    }
}
