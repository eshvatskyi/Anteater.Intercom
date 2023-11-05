using System.Linq.Expressions;

namespace Anteater.Intercom.Core;

public static class Markup
{
    public static TView Spacing<TView>(this TView view, double spacing = 0) where TView : StackBase
    {
        view.Spacing = spacing;
        return view;
    }

    public static string FullName<TSource>(Expression<Func<TSource, object>> expression)
    {
        if (expression.Body is not MemberExpression memberExpression)
        {
            if (expression.Body is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
            }
            else
            {
                return null;
            }
        }

        var path = memberExpression.ToString();

        return path[(path.IndexOf('.') + 1)..];
    }
}
