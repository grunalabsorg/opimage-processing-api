namespace Api.Models
{
    public class SeriesInfoRoot
    {
        public object ExpectedNumberOfInstances { get; set; }
        public string ID { get; set; }
        public List<string> Instances { get; set; }
        public bool IsStable { get; set; }
        public List<object> Labels { get; set; }
        public string LastUpdate { get; set; }
        public MainDicomTags MainDicomTags { get; set; }
        public string ParentStudy { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
    }

    public class MainDicomTags
    {
        public string BodyPartExamined { get; set; }
        public string CardiacNumberOfImages { get; set; }
        public string ContrastBolusAgent { get; set; }
        public string ImageOrientationPatient { get; set; }
        public string ImagesInAcquisition { get; set; }
        public string Manufacturer { get; set; }
        public string Modality { get; set; }
        public string PerformedProcedureStepDescription { get; set; }
        public string ProtocolName { get; set; }
        public string SeriesDate { get; set; }
        public string SeriesDescription { get; set; }
        public string SeriesInstanceUID { get; set; }
        public string SeriesNumber { get; set; }
        public string SeriesTime { get; set; }
        public string StationName { get; set; }
    }
}