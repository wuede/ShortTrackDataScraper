using System.Text.RegularExpressions;

namespace ShortTrackSportResultScraper.Model;

internal class SkaterBio : Skater
{
    internal DateTime Birthday { get; }
    internal Gender Gender { get; }
    internal Dictionary<int, TimeSpan> PersonalBests { get; }

    public SkaterBio(string id, string firstname, string lastname, DateTime birthday, Country nationality, Gender gender,
        Dictionary<int, TimeSpan> personalBests, string url) : base(id, firstname, lastname, nationality, url)
    {
        Birthday = birthday;
        Gender = gender;
        PersonalBests = personalBests;
    }
}

internal class Skater
{
    private static readonly Regex SkaterUrlRegex = new(@"Bio\.aspx\?ath=([A-Z0-9]+)");

    internal string Id { get; }
    internal string Firstname { get; }
    internal string Lastname { get; }
    internal Country Nationality { get; }
    internal string Url { get; }

    internal Skater(string firstname, string lastname, Country nationality, string url) : this("", firstname, lastname, nationality, url)
    {
        var urlMatch = SkaterUrlRegex.Match(url);
        if (urlMatch.Success && urlMatch.Groups.Count > 1)
        {
            Id = urlMatch.Groups[1].Value;
        }
        else
        {
            throw new FormatException($"skater url {url} is of invalid format");
        }
    }

    internal Skater(string id, string firstname, string lastname, Country nationality, string url)
    {
        Id = id;
        Firstname = firstname;
        Lastname = lastname;
        Nationality = nationality;
        Url = url;
    }

    public override string ToString()
    {
        return
            $"{nameof(Id)}: {Id}, {nameof(Firstname)}: {Firstname}, {nameof(Lastname)}: {Lastname}, {nameof(Nationality)}: {Nationality}, {nameof(Url)}: {Url}";
    }
}