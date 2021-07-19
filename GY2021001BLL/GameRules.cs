using GY2021001DAL;
using Gy2021001Template;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
            //ValidationAttribute validationAttribute;
            //IValidatableObject validatableObject;

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

    public class GameValidationResult
    {
        public bool Success { get; set; }

        public GameItem Result { get; set; }

        public string ErrorMessage { get; set; }
    }

    public class VerifyGameItemObject
    {
        public VerifyGameItemObject(Guid? parentTemplateId, Guid? templateId, Guid? id = default)
        {
            ParentTemplateId = parentTemplateId;
            TemplateId = templateId;
            Id = id;
        }

        public Guid? ParentTemplateId { get; set; }

        public Guid? TemplateId { get; set; }

        public Guid? Id { get; set; }

        public int? MinCount { get; set; }

        public int? MaxCount { get; set; }

        public GameItem Result { get; set; }

        public GameValidationResult MatchAndVerify(IServiceProvider services, GameChar gameChar)
        {
            GameValidationResult result = new GameValidationResult();
            IEnumerable<GameItem> parent;
            if (ParentTemplateId.HasValue)
                parent = gameChar.AllChildren.FirstOrDefault(c => c.TemplateId == ParentTemplateId.Value)?.AllChildren;
            else
                parent = gameChar.AllChildren;
            if (null == parent)
            {
                result.Success = false;
                result.ErrorMessage = $"无法找到父对象，Number={ParentTemplateId.Value}";
            }
            return result;
        }
    }

}
