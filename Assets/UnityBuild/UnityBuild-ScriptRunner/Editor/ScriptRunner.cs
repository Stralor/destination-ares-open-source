using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

namespace SuperSystems.UnityBuild
{

	public class ScriptRunner : BuildAction, IPreBuildAction, IPreBuildPerPlatformAction, IPostBuildAction, IPostBuildPerPlatformAction
	{
		[FilePath(false, true, "Select program/script to run.")]
		public string scriptPath = "";
		public string scriptArguments = "";
		public bool runInShell = false;

		public override void Execute()
		{
			RunScript(scriptPath, scriptArguments);
		}

		public override void PerBuildExecute(BuildReleaseType releaseType, BuildPlatform platform, BuildArchitecture architecture, BuildDistribution distribution, System.DateTime buildTime, ref BuildOptions options, string configKey, string buildPath)
		{
			string resolvedScriptPath = BuildProject.ResolvePath(scriptPath, releaseType, platform, architecture, distribution, buildTime);
			string resolvedScriptArgs = BuildProject.ResolvePath(scriptArguments, releaseType, platform, architecture, distribution, buildTime);

			RunScript(resolvedScriptPath, resolvedScriptArgs);
		}

		private void RunScript(string scriptPath, string arguments)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = Path.GetFullPath(scriptPath);
			startInfo.WorkingDirectory = Path.GetDirectoryName(scriptPath);
			startInfo.UseShellExecute = runInShell;

			if (!string.IsNullOrEmpty(arguments))
				startInfo.Arguments = arguments;

			Process proc = Process.Start(startInfo);
			proc.WaitForExit();
		}
	}

}
