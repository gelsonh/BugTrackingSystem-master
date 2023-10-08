namespace BugTrackingSystem.Models.ViewModels
{
    public class ChartData
    {
        public string[]? Labels { get; set; }
        public Dataset[]? Datasets { get; set; }
    }

    public class Dataset
    {
        public string? Label { get; set; }
        public int[]? Data { get; set; }
        public string? FillColor { get; set; }
    }

}
