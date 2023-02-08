namespace Anteater.Intercom.Gui;

using System.Linq.Expressions;
using Sharp.UI;

public static class SharpUIExtensions
{
    public static VisualElement Width(this VisualElement obj, Func<BindingBuilder<VisualElement>, BindingBuilder<VisualElement>> buildBinding)
    {
        var obj2 = MauiWrapper.Value<VisualElement>(obj);
        var bindingBuilder = buildBinding(new BindingBuilder<VisualElement>(obj2, VisualElement.WidthProperty));
        bindingBuilder.BindProperty();
        return obj;
    }

    public static VisualElement Height(this VisualElement obj, Func<BindingBuilder<VisualElement>, BindingBuilder<VisualElement>> buildBinding)
    {
        var obj2 = MauiWrapper.Value<VisualElement>(obj);
        var bindingBuilder = buildBinding(new BindingBuilder<VisualElement>(obj2, VisualElement.HeightProperty));
        bindingBuilder.BindProperty();
        return obj;
    }

    public static Binding Path<TSource>(this Binding obj, Expression<Func<TSource, object>> expression)
    {
        obj.Path(FullName(expression));
        return obj;
    }

    public static BindingBuilder<T> Path<T, TSource>(this BindingBuilder<T> obj, Expression<Func<TSource, object>> expression)
    {
        obj.Path(FullName(expression));
        return obj;
    }

    static string FullName<TSource>(Expression<Func<TSource, object>> expression)
    {
        if (expression.Body is not MemberExpression memberExpression)
        {
            if (expression.Body is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
            } else
            {
                return null;
            }            
        }

        var path = memberExpression.ToString();

        return path[(path.IndexOf('.') + 1)..];
    }
}
