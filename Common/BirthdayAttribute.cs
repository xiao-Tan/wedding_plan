using System;
using System.ComponentModel.DataAnnotations;

namespace WeddingPlan.Common
{
    public class BirthdayAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            DateTime datetime = Convert.ToDateTime(value);
            if (datetime.Date >= DateTime.Now.Date)
            {
                return new ValidationResult("Birthday should be in the past!");
            }
            if ( (DateTime.Now.Date - datetime.Date).TotalDays / 365 < 18)
            {
                return new ValidationResult("Sorry! See you until 18-year old!");
            }
            return ValidationResult.Success;
        }
    }
}