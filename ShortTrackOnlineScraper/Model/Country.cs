using System.Text.RegularExpressions;

namespace ShortTrackOnlineScraper.Model
{
    internal class Country
    {
        private static readonly Regex CountryCodeRegex = new("country=([A-Z]+)");

        internal string Code { get; }
        internal string Url { get; }
        public static Country[] AllCountries { get; set; }

        internal Country(string url)
        {
            var match = CountryCodeRegex.Match(url);
            if (match.Success && match.Groups.Count>1)
            {
                Code = match.Groups[1].Value;
                Url = url;
            }
            else
            {
                throw new FormatException("unexpected format of url");
            }
        }

        public override string ToString()
        {
            return $"{nameof(Code)}: {Code}, {nameof(Url)}: {Url}";
        }
    }
}
