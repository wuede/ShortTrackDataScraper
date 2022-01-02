namespace ShortTrackLiveScraper.Model
{
    internal class Country
    {
        internal static readonly Country[] AllCountries =
        {
            new(147, "ARG"),
            new(173, "ARM"),
            new(141, "AUS"),
            new(1, "AUT"),
            new(146, "AZE"),
            new(81, "BEL"),
            new(5, "BIH"),
            new(7, "BLR"),
            new(174, "BRA"),
            new(8, "BUL"),
            new(144, "CAN"),
            new(142, "CHN"),
            new(10, "CRO"),
            new(11, "CZE"),
            new(175, "DEN"),
            new(108, "ESP"),
            new(14, "EST"),
            new(16, "FIN"),
            new(83, "FRA"),
            new(79, "GBR"),
            new(19, "GER"),
            new(176, "GRE"),
            new(149, "HKG"),
            new(22, "HUN"),
            new(216, "INA"),
            new(177, "IND"),
            new(178, "IRL"),
            new(24, "ISR"),
            new(82, "ITA"),
            new(140, "JPN"),
            new(72, "KAZ"),
            new(143, "KOR"),
            new(31, "LAT"),
            new(32, "LTU"),
            new(179, "LUX"),
            new(167, "MAS"),
            new(180, "MEX"),
            new(136, "MGL"),
            new(181, "MNE"),
            new(80, "NED"),
            new(182, "NOR"),
            new(150, "NZL"),
            new(242, "PHI"),
            new(41, "POL"),
            new(105, "PRK"),
            new(235, "QAT"),
            new(43, "ROU"),
            new(183, "RSA"),
            new(145, "RUS"),
            new(166, "SGP"),
            new(46, "SLO"),
            new(44, "SRB"),
            new(109, "SUI"),
            new(53, "SVK"),
            new(171, "SWE"),
            new(165, "THA"),
            new(148, "TPE"),
            new(106, "TUR"),
            new(59, "UKR"),
            new(151, "USA"),
            new(184, "UZB"),
        };

        internal int Id { get; }
        internal string Code { get; }

        internal Country(int id, string code)
        {
            Id = id;
            Code = code;
        }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(Code)}: {Code}";
        }
    }
}
