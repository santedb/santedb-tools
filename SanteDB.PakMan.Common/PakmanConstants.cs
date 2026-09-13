using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.PakMan
{
    /// <summary>
    /// Constants
    /// </summary>
    public static class PakmanConstants
    {
        /// <summary>
        /// Inject CSP headers into HTML
        /// </summary>
        public const string InjectCspIntoHtml = "csp.inject";

        /// <summary>
        /// Allow non XHTML files
        /// </summary>
        public const string AllowMalformedHtml = "html.loose";

        /// <summary>
        /// Optimize images using method
        /// </summary>
        public const string ImageOptimizationMethod = "img.optimize";

        /// <summary>
        /// Ignore files matching the pattern
        /// </summary>
        public const string SkipPackingAssets = "files.ignore";
    }
}
