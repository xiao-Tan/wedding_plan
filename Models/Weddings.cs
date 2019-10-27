using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using WeddingPlan.Common;

namespace WeddingPlan.Models
{
    public class Wedding
    {
        [Key]
        public int WeddingId { get; set; }

        [Required]
        public string Bride { get; set; }

        [Required]
        public string Bridegroom { get; set; }

        [Required]
        [Date]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        public string WeddingAddress { get; set; }

        [Required]
        public int UserId {get;set;}

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public User Creator {get;set;}

        public List<Association> ManyGuests { get; set; }


    }
}