using GY2021001DAL;
using Gy2021001Template;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GY2021001BLL
{
    public enum GameRulesKind
    {
        And,
        Or,
        Custom,
    }

    public class GameRules
    {
        public GameRules()
        {

        }

        public GameRulesKind Kind { get; set; }

        public List<Guid> Ids { get; set; }

        public int UplimitCount { get; set; }

        public int LowlimitCount { get; set; }

        public Func<bool> Judgment { get; set; }

        public bool Verify(IEnumerable<Guid> ids)
        {
            return true;
        }


    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class LoginNameAttribute : ValidationAttribute
    {
        public LoginNameAttribute()
        {
            ValidationAttribute validationAttribute;
            IValidatableObject validatableObject;

        }

        // This is a positional argument

        public LoginNameAttribute(Func<string> errorMessageAccessor) : base(errorMessageAccessor)
        {
        }

        public LoginNameAttribute(string errorMessage) : base(errorMessage)
        {
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            try
            {
                var world = validationContext.GetService<VWorld>();
                if (value is IEnumerable<Guid> ids)
                {

                }
                else if (value is IEnumerable<GameItem> gameItems)
                {

                }
            }
            catch (Exception)
            {

                throw;
            }
            return ValidationResult.Success;
        }
    }
}
