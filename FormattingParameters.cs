using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KeySAV2
{
    struct FormattingParameters
    {
        public enum GhostMode { Hide, Mark, None}

        public string formatString;
        public string header;
        public GhostMode ghost;
        public bool boldIVs;

        public FormattingParameters(string header, string formatString, GhostMode ghost, bool boldIVs)
        {
            this.header = header;
            this.formatString = formatString;
            this.ghost = ghost;
            this.boldIVs = boldIVs;
        }

        public FormattingParameters(string header, string formatString) : this(header, formatString, GhostMode.Mark, false) {}
        public FormattingParameters(string header, string formatString, GhostMode ghost) : this(header, formatString, ghost, false) {}
        public FormattingParameters(string header, string formatString, bool boldIVs) : this(header, formatString, GhostMode.Mark, boldIVs) {}
    }
}
