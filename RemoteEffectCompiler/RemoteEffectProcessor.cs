using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace RemoteEffectCompiler
{
	[ContentProcessor(DisplayName = "Remote Effect Compiler")]
	class RemoteEffectProcessor : EffectProcessor
	{
		[DefaultValue("example.com")]
		public string Host { get; set; }

		[DefaultValue("44321")]
		public string Port { get; set; }

		public RemoteEffectProcessor()
		{
			Host = "example.com";
			Port = "44321";
		}

		public override CompiledEffectContent Process(EffectContent input, ContentProcessorContext context)
		{
			// process locally if possible
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				return base.Process(input, context);

			var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
			var filename = Path.GetFileNameWithoutExtension(input.Identity.SourceFilename);
			var builder = new UriBuilder(scheme: "http", host: Host, port: int.Parse(Port), pathValue: $"/compiler");
			builder.Query = $"defines={Defines ?? ""};";
			var inlined = FileWithInlinedIncludes(input.Identity.SourceFilename);
			var response = client.PostAsync(builder.Uri, new StringContent(inlined)).Result;

			if (!response.IsSuccessStatusCode)
			{
				string errorDescription;
				if (response.Content.Headers.ContentType.MediaType == "application/json")
				{
					var details = new DataContractJsonSerializer(typeof(ProblemDetails))
						.ReadObject(response.Content.ReadAsStreamAsync().Result) as ProblemDetails;
					errorDescription = details.detail;
				}
				else
				{
					errorDescription = response.Content.ReadAsStringAsync().Result;
				}
				throw new InvalidContentException(
					$"Remote compiler failed with status code {response.StatusCode}:\n{errorDescription}",
					input.Identity);
			}

			var bytecode = response.Content.ReadAsByteArrayAsync().Result;
			return new(bytecode);
		}

		string FileWithInlinedIncludes(string path)
		{
			var lines = File.ReadAllLines(path);
			var builder = new StringBuilder();
			foreach (var line in lines)
			{
				if (line.StartsWith("#include"))
				{
					const string includeRegex = @"#include\s+""(?<path>[^""]+)""";
					var match = Regex.Match(line, includeRegex);
					if (!match.Success)
						throw new InvalidContentException(
							$"Invalid include directive: {line}",
							contentIdentity: new(path));
					var relativePath = match.Groups["path"].Value;
					var baseFolder = Path.GetDirectoryName(path);
					var absolutePath = Path.Combine(baseFolder, relativePath);
					builder.Append(FileWithInlinedIncludes(absolutePath));
				}
				else
				{
					builder.AppendLine(line);
				}
			}
			return builder.ToString();
		}
	}
}
