using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class TrackingQuantity : Tracking
    {
        [Required]
        public string Product { get; set; }
    }
}
