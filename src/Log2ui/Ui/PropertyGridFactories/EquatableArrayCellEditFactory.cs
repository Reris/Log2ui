using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.PropertyGrid.Controls;
using Avalonia.PropertyGrid.Controls.Factories;
using Avalonia.PropertyGrid.Controls.Factories.Builtins;
using Avalonia.PropertyGrid.Services;
using JetBrains.Annotations;
using Log2ui.Collections;
using Log2ui.Dependencies;

namespace Log2ui.Ui.PropertyGridFactories;

[UsedImplicitly]
public class EquatableArrayCellEditFactory : AbstractCellEditFactory, ISelfRegistering
{
    private static readonly MethodInfo GenericWrapList =
        typeof(EquatableArrayCellEditFactory).GetMethod(nameof(EquatableArrayCellEditFactory.WrapList), BindingFlags.Static | BindingFlags.NonPublic)
                                             ?.GetGenericMethodDefinition() ?? throw new InvalidOperationException();

    private readonly BindingListCellEditFactory _bindingListFactory = new();
    public override int ImportPriority => this._bindingListFactory.ImportPriority;

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        CellEditFactoryService.Default.AddFactory(new EquatableArrayCellEditFactory());
    }

    private static Type? GetElementType(PropertyDescriptor pd)
    {
        var propertyType = pd.PropertyType;
        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            propertyType = propertyType.GetGenericArguments()[0];
        }

        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(EquatableArray<>))
        {
            return propertyType.GetGenericArguments()[0];
        }

        return null;
    }

    public override Control? HandleNewProperty(PropertyCellContext context)
    {
        if (EquatableArrayCellEditFactory.GetElementType(context.Property) is not { } elementType)
        {
            return null;
        }

        var wrappedContext = this.WrapContext(context, elementType);
        var control = wrappedContext.CellEdit = this._bindingListFactory.HandleNewProperty(wrappedContext);

        return control;
    }

    public override bool HandlePropertyChanged(PropertyCellContext context)
    {
        if (EquatableArrayCellEditFactory.GetElementType(context.Property) is not { } elementType)
        {
            return false;
        }

        if (context.CellEdit is ListEdit ae)
        {
            var array = context.GetValue() as IEquatableArray;
            if (array is not null && ae.DataList is not null && array.Cast<object>().SequenceEqual(ae.DataList.Cast<object>()))
            {
                return true;
            }

            var value = EquatableArrayCellEditFactory.InvokeWrapList(array, elementType, a => context.Property.SetValue(context.Target, a));
            ae.DataList = value;
            context.CellEdit.IsVisible = value.AllowEdit;

            return true;
        }

        return false;
    }

    public override void HandleReadOnlyStateChanged(Control control, bool readOnly)
    {
        this._bindingListFactory.HandleReadOnlyStateChanged(control, readOnly);
    }

    private PropertyCellContext WrapContext(PropertyCellContext context, Type elementType)
    {
        var wrappedProperty = new WrappedPropertyDescriptor(context.Property, elementType);
        var result = new PropertyCellContext(context.ParentContext, context.Root!, context.Owner, context.Target, wrappedProperty, context.CellEdit);
        return result;
    }

    private static IBindingList InvokeWrapList(IEquatableArray? list, Type elementType, Action<IEquatableArray?> setProperty)
    {
        return (IBindingList)EquatableArrayCellEditFactory.GenericWrapList.MakeGenericMethod(elementType)
                                                          .Invoke(null, [list, setProperty])!;
    }

    private static BindingList<T> WrapList<T>(IEquatableArray? list, Action<IEquatableArray?> setProperty)
        where T : IEquatable<T>
    {
        EquatableArray<T>? value = list is EquatableArray<T> a ? a : null;
        if (!value.HasValue)
        {
            // Return an empty List so the BindingListCellEditFactory can create a control
            return new BindingList<T> { AllowEdit = false, AllowNew = false, AllowRemove = false };
        }

        var clone = new BindingList<T>(value.Value.ToList());
        clone.ListChanged += (_, _) => setProperty(new EquatableArray<T>(clone.ToArray()));
        return clone;
    }

    private class WrappedPropertyDescriptor(PropertyDescriptor inner, Type elementType) : PropertyDescriptor(
        inner.Name,
        inner.Attributes.Cast<Attribute>().ToArray())
    {
        public override Type ComponentType => inner.ComponentType;
        public override bool IsReadOnly => inner.IsReadOnly;
        public override Type PropertyType { get; } = typeof(BindingList<>).MakeGenericType(elementType);

        public override bool CanResetValue(object component)
        {
            throw new NotSupportedException(nameof(this.CanResetValue));
        }

        public override object GetValue(object? component)
        {
            var array = (IEquatableArray?)inner.GetValue(component);
            return EquatableArrayCellEditFactory.InvokeWrapList(array, elementType, a => inner.SetValue(component, a));
        }

        public override void ResetValue(object component)
        {
            throw new NotSupportedException(nameof(this.ResetValue));
        }

        public override void SetValue(object? component, object? value)
        {
            throw new NotSupportedException(nameof(this.SetValue));
        }

        public override bool ShouldSerializeValue(object component)
        {
            return inner.ShouldSerializeValue(component);
        }
    }
}
