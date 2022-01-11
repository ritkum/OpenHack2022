using Newtonsoft.Json;
namespace Models
{
    public class Ratings
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }
        public string userId { get; set; }
        public string productId { get; set; }
        public string locationName { get; set; }
        public string timeStamp { get; set; }
        public int rating { get; set; }
        public string userNotes { get; set; }       

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}