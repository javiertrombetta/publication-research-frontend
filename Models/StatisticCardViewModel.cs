namespace ResearchPublicationManagementSystem.Models
{
    public class StatisticCardViewModel
    {
        public string Title { get; set; } = "";

        public int Value { get; set; }

        public string ColorClass { get; set; } = "text-primary";

        public string Icon { get; set; } = "users";

        public string IconBackground { get; set; } = "bg-primary-lt";
    }
}