using System;

namespace TestVulkan
{
	public class PipelineT
	{
		public PipelineT(string vertPath, string fragPath)
		{
			CreateGraphicsPipeline(vertPath, fragPath);
		}

		private static byte[] ReadFile(string filename)
		{
			byte[] bytes = File.ReadAllBytes(filename);

			if (bytes == null)
				throw new Exception("file is null: " + filename);

			return bytes;
		}

		private void CreateGraphicsPipeline(string vertPath, string fragPath) 
		{
			byte[] vertShaderCode = ReadFile(vertPath);
			byte[] fragShaderCode = ReadFile(fragPath);


		}
	}
}
