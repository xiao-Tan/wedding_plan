using System.ComponentModel.DataAnnotations;


namespace WeddingPlan.Models
{
    public class Association
    {
        [Key]
        public int AssociationId { get; set; }

        public int UserId { get; set; }

        public int WeddingId { get; set; }

        public User User { get; set; }

        public Wedding Wedding { get; set; }
    }
}