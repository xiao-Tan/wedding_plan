using System;
using System.ComponentModel.DataAnnotations;

namespace WeddingPlan.Common
{
    public class DateAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            DateTime datetime = Convert.ToDateTime(value);
            if (datetime.Date <= DateTime.Now.Date)
            {
                return new ValidationResult("Birthday should be in the future!");
            }
            return ValidationResult.Success;
        }
    }
}