using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RazorLight.Razor;
using RazorLight.Tests.Utils;
using Xunit;

namespace RazorLight.Tests.Razor
{
	public class FileSystemRazorProjectTest
	{
		[Fact]
		public void NotExiting_RootDirectory_Throws()
		{
			void Action() => _ = new FileSystemRazorProject(@"C:/Not/Existing/Folder/Here");

			Assert.Throws<DirectoryNotFoundException>(Action);
		}

		[Fact]
		public void Ensure_RootProperty_AssignedOnConstructor()
		{
			string root = Path.Combine(DirectoryUtils.RootDirectory, "Assets", "Files");

			var project = new FileSystemRazorProject(root);

			Assert.Equal(root, project.Root);
		}

		[Fact]
		public void Ensure_ExtensionProperty_IsDefaultIfNotProvided()
		{
			string root = Path.Combine(DirectoryUtils.RootDirectory, "Assets", "Files");

			var project = new FileSystemRazorProject(root);

			Assert.Equal(FileSystemRazorProject.DefaultExtension, project.Extension);
		}

		[Fact]
		public void Ensure_ExtensionProperty_AssignedOnConstructor()
		{
			string root = Path.Combine(DirectoryUtils.RootDirectory, "Assets", "Files");
			string extension = FileSystemRazorProject.DefaultExtension + "_test";

			var project = new FileSystemRazorProject(root, extension);

			Assert.Equal(project.Extension, extension);
		}

		[Fact]
		public async Task Null_TemplateKey_ThrowsOn_GetItem()
		{
			var project = new FileSystemRazorProject(DirectoryUtils.RootDirectory);

			await Assert.ThrowsAsync<ArgumentNullException>(() => project.GetItemAsync(null!));
		}

		[Fact]
		public async Task Ensure_TemplateKey_IsNormalizedAsync()
		{
			var project = new FileSystemRazorProject(DirectoryUtils.RootDirectory);

			string templateKey = "Empty";

			var item = await project.GetItemAsync(Path.Combine("Assets", "Embedded", templateKey));

			Assert.NotNull(item);
			Assert.EndsWith(templateKey + project.Extension, item.Key);
		}

		[Theory]
		[InlineData("../Embedded/Empty.cshtml")]
		[InlineData("..\\Embedded\\Empty.cshtml")]
		[InlineData("Subfolder/../../../Embedded/Empty.cshtml")]
		public async Task GetItemAsync_Rejects_Keys_Outside_Project_Root(string templateKey)
		{
			string root = Path.Combine(DirectoryUtils.RootDirectory, "Assets", "Files");
			var project = new FileSystemRazorProject(root);

			await Assert.ThrowsAsync<InvalidOperationException>(() => project.GetItemAsync(templateKey));
		}

		[Fact]
		public async Task GetItemAsync_Confines_Absolute_Style_Path_To_Project_Root()
		{
			string root = Path.Combine(DirectoryUtils.RootDirectory, "Assets", "Files");
			string outside = Path.Combine(DirectoryUtils.RootDirectory, "Assets", "Embedded", "Empty.cshtml");
			var project = new FileSystemRazorProject(root);

			if (OperatingSystem.IsWindows())
			{
				await Assert.ThrowsAsync<InvalidOperationException>(() => project.GetItemAsync(outside));
				return;
			}

			var item = Assert.IsType<FileSystemRazorProjectItem>(await project.GetItemAsync(outside));
			Assert.False(item.Exists);
			Assert.StartsWith(
				Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar,
				item.File.FullName,
				StringComparison.Ordinal);
			Assert.NotEqual(Path.GetFullPath(outside), item.File.FullName);
		}

		[Theory]
		[InlineData("/Empty.cshtml")]
		[InlineData("\\Empty.cshtml")]
		[InlineData("./Empty.cshtml")]
		public async Task GetItemAsync_Allows_Contained_Virtual_Paths(string templateKey)
		{
			string root = Path.Combine(DirectoryUtils.RootDirectory, "Assets", "Files");
			var project = new FileSystemRazorProject(root);

			RazorLightProjectItem item = await project.GetItemAsync(templateKey);

			Assert.True(item.Exists);
		}

		[Fact]
		public async Task GetSourceItemAsync_Rejects_Sibling_Prefix_Path()
		{
			string root = Path.Combine(DirectoryUtils.RootDirectory, "Assets", "Files");
			string sibling = root + "Sibling";
			var project = new FileSystemRazorProject(root);

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				project.GetSourceItemAsync(Path.Combine(sibling, "Shared.cs")));
		}

		[Fact]
		public async Task Ensure_GetKnownKeysAsync_Returns_Existing_Keys()
		{
			var project = new FileSystemRazorProject(DirectoryUtils.RootDirectory);

			var knownKeys = (await project.GetKnownKeysAsync()).ToList();
			Assert.NotNull(knownKeys);
			Assert.NotEmpty(knownKeys);

			foreach (var key in knownKeys)
			{
				var projectItem = await project.GetItemAsync(key);
				Assert.True(projectItem.Exists);
			}
		}

		[Fact]
		public async Task Ensure_GetKnownKeysAsync_Returns_Expected_Keys()
		{
			var subsetToCheck = new[]
			{
				"Assets/Files/Empty.cshtml",
				"Assets/Files/Layout.cshtml"
			};

			var project = new FileSystemRazorProject(DirectoryUtils.RootDirectory);

			var knownKeys = (await project.GetKnownKeysAsync()).ToList();
			Assert.NotNull(knownKeys);
			Assert.NotEmpty(knownKeys);

			foreach (var key in subsetToCheck)
			{
				Assert.Contains(key, knownKeys);
			}
		}
	}
}
