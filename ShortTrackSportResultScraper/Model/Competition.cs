namespace ShortTrackSportResultScraper.Model;

internal class Competition
{
    public string Id { get; }
    public string Name { get; }

    public Competition(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public override string ToString()
    {
        return $"{nameof(Id)}: {Id}, {nameof(Name)}: {Name}";
    }
}