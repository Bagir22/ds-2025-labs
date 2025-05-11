namespace Valuator.Models
{
    public enum RegionCode
    {
        RU,
        EU,
        ASIA
    }

    public class Country
    {
        public string Name { get; set; }
        public RegionCode Region { get; set; }

        public static List<Country> AllCountries => new()
        {
            new Country { Name = "Russia", Region = RegionCode.RU },
            new Country { Name = "France", Region = RegionCode.EU },
            new Country { Name = "Germany", Region = RegionCode.EU },
            new Country { Name = "UAE", Region = RegionCode.ASIA },
            new Country { Name = "India", Region = RegionCode.ASIA },
        };
    }
}