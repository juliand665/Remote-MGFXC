using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IOFile = System.IO.File;

namespace EffectCompiler.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class CompilerController : ControllerBase
	{
		[HttpPost]
		public async Task<ActionResult<string>> Compile(string defines = "")
		{
			var inputPath = Path.GetTempFileName();
			var outputPath = Path.GetTempFileName();
			IOFile.Delete(outputPath);

			try
			{
				using (var inputFile = IOFile.OpenWrite(inputPath))
					await Request.Body.CopyToAsync(inputFile);
				var lines = IOFile.ReadAllLines(inputPath);

				System.Diagnostics.Process cmd = new();
				cmd.StartInfo.FileName = "cmd.exe";
				cmd.StartInfo.RedirectStandardInput = true;
				cmd.StartInfo.RedirectStandardOutput = true;
				cmd.StartInfo.RedirectStandardError = true;
				cmd.StartInfo.CreateNoWindow = true;
				cmd.StartInfo.UseShellExecute = false;
				cmd.Start();

				var args = new StringBuilder();
				const string defineRegex = @"^[\w\d_\-.=;]*$";
				if (!Regex.IsMatch(defines, defineRegex))
					return BadRequest("Invalid defines: " + defines);
				args.Append($"/Defines:{defines}");

				cmd.StandardInput.WriteLine("mgfxc {0} {1} {2}", inputPath, outputPath, args);
				cmd.StandardInput.Flush();
				cmd.StandardInput.Close();
				await cmd.WaitForExitAsync();

				if (cmd.ExitCode == 0 && IOFile.Exists(outputPath))
				{
					var fileStream = IOFile.OpenRead(outputPath);
					return File(fileStream, "application/octet-stream");
				}
				else
				{
					string output = cmd.StandardError.ReadToEnd();
					var fullFilePath = Path.Combine(Directory.GetCurrentDirectory(), inputPath)
						.Replace("\\", "\\\\"); // lmao why
					var strippedLines = new List<string>();
					foreach (var line in output.Split(Environment.NewLine))
					{
						if (line.StartsWith(fullFilePath))
						{
							var lineWithoutPath = line.Substring(fullFilePath.Length);
							var space = lineWithoutPath.IndexOf(' ');
							if (space > 0)
							{
								var lineNumber = int.Parse(lineWithoutPath[1..lineWithoutPath.IndexOf(',')]);
								strippedLines.Add("");
								strippedLines.Add($"> {lines[lineNumber - 1]}");
							}
							strippedLines.Add($"Effect.fx{lineWithoutPath}");
						}
						else
						{
							strippedLines.Add(line);
						}
					}
					var error = string.Join(Environment.NewLine, strippedLines);
					return Problem(title: "Error executing MGFXC", detail: error);
				}
			}
			finally
			{
				//IOFile.Delete(inputPath);
				//IOFile.Delete(outputPath);
			}
		}
	}
}
