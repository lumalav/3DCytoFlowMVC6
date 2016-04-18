namespace _3DCytoFlow.Models
{
    public class RequestResponse
    {
        public RequestResponse()
        {
            FileLocation = "";
            Found = false;
            Jobs = 1;
            Points = 10;
        }

        public string FileLocation { get; set; }
        public bool Found { get; set; }
        public int Jobs { get; set; }
        public int Points { get; set; }
    }
}