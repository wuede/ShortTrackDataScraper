namespace ShortTrackLiveScraper.Model;

internal class Skater
{
    internal string Firstname { get; }
    internal string Lastname { get; }
    internal Country Country { get; }
    internal Gender Gender { get; }
    internal int YearOfBirth { get; }
    internal string AgeCategory { get; }
    internal string Club { get; }
    internal List<Tuple<int, TimeSpan>> PersonalBests { get; }
    internal string Url { get; }

    public Skater(string firstname, string lastname, Country country, Gender gender, int yearOfBirth, string ageCategory, string club,
        List<Tuple<int, TimeSpan>> personalBests, string url)
    {
        Firstname = firstname;
        Lastname = lastname;
        Country = country;
        Gender = gender;
        YearOfBirth = yearOfBirth;
        AgeCategory = ageCategory;
        Club = club;
        PersonalBests = personalBests;
        Url = url;
    }
}