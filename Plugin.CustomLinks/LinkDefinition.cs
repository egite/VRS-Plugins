using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualRadar.Plugin.CustomLinks
{
    /// <summary>
    /// Describes a single custom link with a name and URL template.
    /// </summary>
    public class LinkDefinition
    {
        /// <summary>
        /// Gets or sets the display name for the link.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the URL template. Supports placeholders like {icao}, {reg}, {callsign}.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets an optional ICAO prefix condition. When set, the link is only shown
        /// for aircraft whose ICAO hex code starts with this value.
        /// </summary>
        public string ConditionPrefix { get; set; }

        /// <summary>
        /// Creates a new object.
        /// </summary>
        public LinkDefinition()
        {
        }
    }
}
