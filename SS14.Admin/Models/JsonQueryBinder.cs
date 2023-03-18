using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SS14.Admin.Models
{
    /// <summary>
    /// Deserializes and binds values from a json query parameter to a property
    /// e.g. public List&lt;AdminLogFilterModel&gt; Filter;
    /// Filter is the property name and the corresponding query parameter gets parsed into the type. In this example: List&gt;AdminLogFilterModel&gt;
    /// </summary>
    public class JsonQueryBinder: IModelBinder
    {
        private readonly IObjectModelValidator _validator;


        public JsonQueryBinder(IObjectModelValidator validator)
        {
            _validator = validator;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            //Get query value to be bound, FieldName being the property name this binder is used on
            var value = bindingContext.ValueProvider.GetValue(bindingContext.FieldName).FirstValue;
            if (value == null)
                return Task.CompletedTask;

            try
            {
                var parsed = JsonSerializer.Deserialize(value, bindingContext.ModelType,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

                bindingContext.Result = ModelBindingResult.Success(parsed);

                if (parsed != null)
                {
                    _validator.Validate(
                        bindingContext.ActionContext,
                        bindingContext.ValidationState,
                        string.Empty,
                        parsed
                    );
                }
            }
            catch (JsonException e)
            {
                bindingContext.ActionContext.ModelState.TryAddModelError(e.Path ?? string.Empty, e,
                    bindingContext.ModelMetadata);
            }
            catch (Exception e) when (e is FormatException or OverflowException)
            {
                bindingContext.ActionContext.ModelState.TryAddModelError(string.Empty, e, bindingContext.ModelMetadata);
            }

            return Task.CompletedTask;
        }
    }
}
