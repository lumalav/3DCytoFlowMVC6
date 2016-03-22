namespace _3DCytoFlow.Models
{
    public class RequestResponse
    {
        public RequestResponse()
        {
            FileLocation = "";
            Found = false;
        }

        public string FileLocation { get; set; }
        public bool Found { get; set; }
    }
}