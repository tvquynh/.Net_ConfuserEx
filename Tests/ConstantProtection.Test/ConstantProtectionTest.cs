using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.UnitTest;
using Xunit;
using Xunit.Abstractions;

namespace CompressorWithResx.Test {
	public sealed class ConstantProtectionTest {
		private readonly ITestOutputHelper outputHelper;

		public ConstantProtectionTest(ITestOutputHelper outputHelper) =>
			this.outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));

		[Theory]
		[MemberData(nameof(ProtectAndExecuteTestData))]
		[Trait("Category", "Protection")]
		[Trait("Protection", "constants")]
		public async Task ProtectAndExecuteTest(string modeKey, bool cfgKey, string elementsKey) {
			var baseDir = Environment.CurrentDirectory;
			var outputDir = Path.Combine(baseDir, "testtmp");
			var inputFile = Path.Combine(baseDir, "ConstantProtection.exe");
			var outputFile = Path.Combine(outputDir, "ConstantProtection.exe");
			FileUtilities.ClearOutput(outputFile);
			var proj = new ConfuserProject {
				BaseDirectory = baseDir,
				OutputDirectory = outputDir
			};

			proj.Rules.Add(new Rule() {
				new SettingItem<IProtection>("constants") {
					{ "mode", modeKey },
					{ "cfg", cfgKey ? "true" : "false" },
					{ "elements", elementsKey }
				}
			});

			proj.Add(new ProjectModule() { Path = inputFile });


			var parameters = new ConfuserParameters {
				Project = proj,
				Logger = new XunitLogger(outputHelper)
			};

			await ConfuserEngine.Run(parameters);

			Assert.True(File.Exists(outputFile));
			Assert.NotEqual(FileUtilities.ComputeFileChecksum(inputFile), FileUtilities.ComputeFileChecksum(outputFile));

			var info = new ProcessStartInfo(outputFile) {
				RedirectStandardOutput = true,
				UseShellExecute = false
			};
			using (var process = Process.Start(info)) {
				var stdout = process.StandardOutput;
				Assert.Equal("START", await stdout.ReadLineAsync());
				Assert.Equal("123456", await stdout.ReadLineAsync());
				Assert.Equal("3", await stdout.ReadLineAsync());
				Assert.Equal("Test3", await stdout.ReadLineAsync());
				Assert.Equal("END", await stdout.ReadLineAsync());
				Assert.Empty(await stdout.ReadToEndAsync());
				Assert.True(process.HasExited);
				Assert.Equal(42, process.ExitCode);
			}

			FileUtilities.ClearOutput(outputFile);
		}

		public static IEnumerable<object[]> ProtectAndExecuteTestData() {
			foreach (var mode in new string[] { "Normal", "Dynamic", "x86" })
				foreach (var cfg in new bool[] { false, true })
					foreach (var encodeStrings in new string[] { "", "S" })
						foreach (var encodeNumbers in new string[] { "", "N" })
							foreach (var encodePrimitives in new string[] { "", "P" })
								foreach (var encodeInitializers in new string[] { "", "I" })
									yield return new object[] { mode, cfg, encodeStrings + encodeNumbers + encodePrimitives + encodeInitializers };
		}
	}
}