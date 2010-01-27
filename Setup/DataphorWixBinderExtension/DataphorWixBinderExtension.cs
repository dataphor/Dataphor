using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;

using Microsoft.Tools.WindowsInstallerXml;

namespace DataphorWixBinderExtension
{
    /// <summary> </summary>
    public class DataphorWixBinderExtension : BinderExtension
    {
		private StringCollection basePaths;
		private string cabCachePath;
		private bool reuseCabinets;
		private StringCollection sourcePaths;

		/// <summary>
		/// Instantiate a new LightBinderExtension.
		/// </summary>
		/// <param name="basePaths">Base paths to locate files.</param>
		/// <param name="cabCachePath">Path to cabinet cache.</param>
		/// <param name="reuseCabinets">Option to reuse cabinets in the cache.</param>
		/// <param name="sourcePaths">All source paths to intermediate files.</param>
		public DataphorWixBinderExtension(StringCollection basePaths, string cabCachePath, bool reuseCabinets, StringCollection sourcePaths)
		{
			this.basePaths = basePaths;
			this.cabCachePath = cabCachePath;
			this.reuseCabinets = reuseCabinets;
			this.sourcePaths = sourcePaths;
		}

        /// <summary>
        /// FileResolutionHandler callback that looks for a valid path based on where
        /// the ASources were located.
        /// </summary>
        /// <param name="ASource">Original ASource path.</param>
        /// <param name="fileType">Type of file to look for.</param>
        /// <returns>ASource path to be used when importing the file.</returns>
        public override string FileResolutionHandler(string ASource, FileResolutionType fileType)
        {
			string LBasePath = basePaths[0];
            //string[] LArgs = Environment.GetCommandLineArgs();
            //for (int i = 0; i < LArgs.Length; i++)
            //    if (LArgs[i] == "-b")
            //        LBasePath = LArgs[i + 1];

			if (ASource.StartsWith("\\"))
				ASource = ASource.Substring(1);

			if (ASource.StartsWith(@"Binary\") || ASource.StartsWith(@"Icon\"))
			{
				if (File.Exists(ASource))
					return ASource;
			}

			if ((LBasePath != null) && (ASource.StartsWith(@"SourceDir\") || ASource.StartsWith("SourceDir/")))
			{
				string LFilePath = Path.Combine(LBasePath, ASource.Substring(10));
				if (File.Exists(LFilePath))
					return LFilePath;
			}

			return ASource;
        }

        /// <summary>
        /// CabinetResolutionHandler callback that looks for an existing, up to date
        /// cabinet and reuses that instead of rebuilding.
        /// </summary>
        /// <param name="fileIds">Array of file identifiers that will be compressed into cabinet.</param>
        /// <param name="LFilePaths">Array of file paths that will be compressed.  Paired with fileIds array.</param>
        /// <param name="cabinetPath">Path to cabinet to generate.  Path may be modified by delegate.</param>
        /// <returns>False if cabinet should be created, true if cabinet should be reused.</returns>
        public override CabinetBuildOption CabinetResolutionHandler(string[] fileIds, string[] LFilePaths, ref string cabinetPath)
        {
			cabinetPath = Path.Combine(@".\output", Path.GetFileName(cabinetPath));
			return CabinetBuildOption.BuildAndCopy;
        }
    }
}
