using System.ComponentModel.DataAnnotations;

namespace SupportCenter.ApiService.Extensions
{
    /* 
     * This class is used to extend the validation functionality of the application.
     */
    public static class ValidationExtensions
    {
        /* This method is used to validate the required properties of an object.
         * @param obj The object to validate.
         * Returns: void.
         * Throws: ValidationException if the object is not valid.
         */
        public static void ValidateRequiredProperties(this object obj)
        {
            var context = new ValidationContext(obj);
            Validator.ValidateObject(obj, context, validateAllProperties: true);
        }
    }
}
