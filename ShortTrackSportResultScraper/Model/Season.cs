namespace ShortTrackSportResultScraper.Model
{
    internal class Season
    {
        internal string Id { get; }
        internal string Name { get; }

        internal Season(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(Name)}: {Name}";
        }
    }
}
