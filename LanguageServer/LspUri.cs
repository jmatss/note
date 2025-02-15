namespace LanguageServer
{
    public class LspUri
    {
        public LspUri(Uri uri, LspUriType type)
        {
            this.Uri = uri;
            this.Type = type;
        }

        public Uri Uri { get; }

        public LspUriType Type { get; }

        public static LspUri Workspace(string uri) => new LspUri(new Uri(uri), LspUriType.Workspace);

        public static LspUri TextDocument(Uri uri) => new LspUri(uri, LspUriType.TextDocument);

        public static LspUri TextDocument(string uri) => TextDocument(new Uri(uri));

        public override bool Equals(object? obj)
        {
            if (obj == null) { return false;  }
            if (object.ReferenceEquals(this, obj)) { return true; }
            if (obj is not LspUri other) { return false; }
            return other.Type == this.Type && other.Uri == this.Uri;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Type, this.Uri);
        }
    }
}
