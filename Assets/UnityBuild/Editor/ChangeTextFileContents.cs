using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;


namespace SuperSystems.UnityBuild
{
	public sealed class ChangeTextFileContents : BuildAction, IPostBuildPerPlatformAction, IPreBuildPerPlatformAction
	{
		[FilePath(false)]
		public string inputPath;
		[FilePath(false)]
		public string outputPath;
		public bool recursiveSearch = true;
		public string regexToChange;
		[Tooltip("Accepts Super Unity Build's glyphs (e.g., \'$VERSION\')")]
		public string replacementText;


		public override void PerBuildExecute(BuildReleaseType releaseType, BuildPlatform platform, BuildArchitecture architecture, BuildDistribution distribution, System.DateTime buildTime, ref BuildOptions options, string configKey, string buildPath)
		{
			string resolvedInputPath = BuildProject.ResolvePath(inputPath.Replace("$BUILDPATH", buildPath), releaseType, platform, architecture, distribution, buildTime);
			string resolvedOutputPath = BuildProject.ResolvePath(outputPath.Replace("$BUILDPATH", buildPath), releaseType, platform, architecture, distribution, buildTime);

			bool containsWildcard = string.IsNullOrEmpty(resolvedInputPath) ? false : Path.GetFileNameWithoutExtension(resolvedInputPath).Contains("*");

			if (!containsWildcard && !File.Exists(resolvedInputPath))
			{
				// Error. Input does not exist.
				return;
			}


			string inputDirectory = Path.GetDirectoryName(resolvedInputPath);
			string outputDirectory = Path.GetDirectoryName(resolvedOutputPath);

			SearchOption option = recursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
			string[] fileList = Directory.GetFiles(inputDirectory, Path.GetFileName(resolvedInputPath), option);

			for (int i = 0; i < fileList.Length; i++)
			{
				string fileName = Path.GetFileName(fileList [i]);
				string outputFile = resolvedOutputPath;

				if (containsWildcard)
					outputFile = Path.Combine(outputDirectory, fileName);

				//Copy the text
				string text = File.ReadAllText(fileList [i]);
				//Main edit
				text = Regex.Replace(text, regexToChange, replacementText);
				//Backfill with BuildProject's glyphs
				text = BuildProject.ResolvePath(text.Replace("$BUILDPATH", buildPath), releaseType, platform, architecture, distribution, buildTime);
				//Write it out
				File.WriteAllText(outputFile, text);
			}
			AssetDatabase.Refresh();
		}

	}
}
