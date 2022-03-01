﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IOFile = System.IO.File;

namespace EffectCompiler.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CompilerController : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<string>> Compile(string filename)
        {
            Console.WriteLine("Using filename: {0}", filename);
            var tempFolder = "temp_compiler_files";
            Directory.CreateDirectory(tempFolder);
            var inputPath = Path.Combine(tempFolder, filename + ".fx");
            var outputPath = Path.Combine(tempFolder, filename + ".ogl.mgfxo");

            IOFile.Delete(inputPath);
            IOFile.Delete(outputPath);

            try
            {
                using (var inputFile = IOFile.Create(inputPath))
                {
                    await Request.Body.CopyToAsync(inputFile);
                }

                System.Diagnostics.Process cmd = new();
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.RedirectStandardError = true;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.Start();

                cmd.StandardInput.WriteLine("mgfxc {0} {1}", inputPath, outputPath); // /Profile:OpenGL
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
                    return Problem(title: "Error executing MGFXC", detail: output);
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
