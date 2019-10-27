using System.ComponentModel.DataAnnotations;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingPlan.Models
{
    public class LogUser
    {
        [Required]
        [EmailAddress]
        public string LogEmail { get; set; }
        
        [Required]
        [DataType(DataType.Password)]
        public string LogPassword { get; set; }
    }
}