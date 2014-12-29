namespace KeySAV2.Structures
{
    public struct FormattingParameters
    {
        public enum GhostMode { Hide, Mark, None }

        public string formatString;
        public string header;
        public GhostMode ghost;
        public bool boldIVs;
        public bool encloseESV;

        public FormattingParameters(string header, string formatString, GhostMode ghost, bool boldIVs, bool encloseESV)
        {
            this.header = header;
            this.formatString = formatString;
            this.ghost = ghost;
            this.boldIVs = boldIVs;
            this.encloseESV = encloseESV;
        }

        public FormattingParameters() : this("", "", GhostMode.Mark, false, false) {}
        public FormattingParameters(string header, string formatString) : this(header, formatString, GhostMode.Mark, false, false) {}
        public FormattingParameters(string header, string formatString, GhostMode ghost) : this(header, formatString, ghost, false, false) {}
        public FormattingParameters(string header, string formatString, bool boldIVs) : this(header, formatString, GhostMode.Mark, boldIVs, false) {}
        public FormattingParameters(string header, string formatString, GhostMode ghost, bool boldIVs) : this(header, formatString, ghost, boldIVs, false) {}

    }
}
