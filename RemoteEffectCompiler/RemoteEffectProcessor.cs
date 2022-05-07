using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
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
			builder.Query = $"defines={Defines}";
			var response = client.PostAsync(
				builder.Uri,
				new StringContent(input.EffectCode))
				.Result;

			if (!response.IsSuccessStatusCode)
			{
				//var jsonString = response.Content.ReadAsStringAsync().Result;
				//var details = JsonConvert.DeserializeObject<ProblemDetails>(jsonString);
				var details = new DataContractJsonSerializer(typeof(ProblemDetails))
					.ReadObject(response.Content.ReadAsStreamAsync().Result) as ProblemDetails;
				throw new InvalidContentException(
					$"Remote compiler failed with status code {response.StatusCode}:\n{details.detail}",
					input.Identity);
			}

			var bytecode = response.Content.ReadAsByteArrayAsync().Result;
			return new(bytecode);
		}
	}
}
