using System.Text.RegularExpressions;

namespace ShortTrackOnlineScraper.Model;

internal class SkaterBio : Skater
{
    internal Gender Gender { get; }
    internal Dictionary<int, TimeSpan> PersonalBests { get; }
    
    public SkaterBio(string firstname, string lastname, Country country, Gender gender,
        Dictionary<int, TimeSpan> personalBests, string url) : base(firstname, lastname, country, url)
    {
        Gender = gender;
        PersonalBests = personalBests;
    }
}

internal class Skater
{
    private static readonly Regex SkaterUrlRegex = new(@"skaterbio\.php\?id=([A-Z0-9]+)");

    internal string Id { get; }
    internal string Firstname { get; }
    internal string Lastname { get; }
    internal Country Country { get; }
    internal string Url { get; }

    public Skater(string firstname, string lastname, Country country, string url)
    {
        var urlMatch = SkaterUrlRegex.Match(url);
        if (urlMatch.Success && urlMatch.Groups.Count > 1)
        {
            Id = urlMatch.Groups[1].Value;
            Firstname = firstname;
            Lastname = lastname;
            Country = country;
            Url = url;
        }
        else
        {
            throw new FormatException($"skater url {url} is of invalid format");
        }
    }

    public override string ToString()
    {
        return
            $"{nameof(Id)}: {Id}, {nameof(Firstname)}: {Firstname}, {nameof(Lastname)}: {Lastname}, {nameof(Country)}: {Country}, {nameof(Url)}: {Url}";
    }
}