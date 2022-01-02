using System.Text.RegularExpressions;

namespace ShortTrackSportResultScraper.Model
{
    internal class Country
    {
        private static readonly Regex CountryCodeRegex = new("nat=([a-zA-Z]+)&?");

        internal string Code { get; }
        internal string Url { get; }

        internal Country(string url)
        {
            var match = CountryCodeRegex.Match(url);
            if (match.Success && match.Groups.Count>1)
            {
                Code = match.Groups[1].Value.ToUpper();
                Url = url;
            }
            else
            {
                throw new FormatException($"unexpected format of url {Url}");
            }
        }

        public override string ToString()
        {
            return $"{nameof(Code)}: {Code}, {nameof(Url)}: {Url}";
        }
    }
}
