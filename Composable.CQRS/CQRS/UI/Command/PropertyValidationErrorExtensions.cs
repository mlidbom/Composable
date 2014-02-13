using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Composable.CQRS.UI.Command
{
    public static class PropertyValidationErrorExtensions
    {
        public static PropertyValidationError CreateError<T>(this T uiCommand, Expression<Func<T, object>> propertyExpression, string error)
        {
            string propertyName = null;
            var lambda = propertyExpression as LambdaExpression;
            MemberExpression memberExpression = null;
            if (lambda.Body is UnaryExpression)
            {
                var unaryExpression = lambda.Body as UnaryExpression;
                if (unaryExpression != null) 
                    memberExpression = unaryExpression.Operand as MemberExpression;
            }
            else
            {
                memberExpression = lambda.Body as MemberExpression;
            }

            if (memberExpression != null)
            {
                var propertyInfo = memberExpression.Member as PropertyInfo;
                if (propertyInfo != null) propertyName = propertyInfo.Name;
            }
            
            return new PropertyValidationError(propertyName, error);
        }

    }
}