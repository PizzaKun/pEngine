﻿using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using pEngine.Framework.Binding;

namespace pEngine.Framework
{
    public partial class pObject
    {

        private void initializeBinding()
        {
            propagationVectors = new Dictionary<string, List<BindingInformations>>();
            PropertyChanged += checkBindings;
        }

        private void disposeBinding()
        {
            propagationVectors.Clear();
        }

        #region Set bindings

        /// <summary>
        /// Set a binding between a property of this class and a property of a
        /// target class.
        /// </summary>
        /// <param name="source">Source property.</param>
        /// <param name="target">Target <see cref="pObject"/> which have the target property.</param>
        /// <param name="destinationProperty">Target property to bind.</param>
        public void Bind(string source, pObject target, string destinationProperty, BindingMode direction = BindingMode.TwoWay, Func<object, object> adapterToDestination = null, Func<object, object> adapterToSource = null)
        {
            var sourceType = GetType();
            var sourceProperty = sourceType.GetProperty(source);
            var destinationType = GetType();
            var destProperty = sourceType.GetProperty(destinationProperty);

            Bindable sourceAttribute = null;
            Bindable destAttribute = null;

            if (source == destinationProperty && target == this)
                throw new Exception("Cannot bind a property to itself.");

            if (sourceProperty.PropertyType != destProperty.PropertyType)
            {
                switch (direction)
                {
                    case BindingMode.TwoWay:
                        if (adapterToDestination == null || adapterToSource == null)
                            throw new InvalidOperationException("Binding between two different types without adapter.");
                        break;
                    case BindingMode.ReadOnly:
                        if (adapterToDestination == null)
                            throw new InvalidOperationException("Binding between two different types without adapter.");
                        break;
                    case BindingMode.WriteOnly:
                        if (adapterToSource == null)
                            throw new InvalidOperationException("Binding between two different types without adapter.");
                        break;
                }
            }

            try
            {
                sourceAttribute = sourceProperty.GetCustomAttribute(typeof(Bindable)) as Bindable;
                destAttribute = destProperty.GetCustomAttribute(typeof(Bindable)) as Bindable;
            }
            catch (NullReferenceException)
            {
                throw new InvalidOperationException("One of this properties has no bindable attribute");
            }

            if (sourceAttribute == null || destAttribute == null)
                throw new InvalidOperationException("One of this properties has no bindable attribute");

            switch (direction)
            {
                case BindingMode.TwoWay:
                    if (sourceAttribute.Direction != BindingMode.TwoWay
                    ||  destAttribute.Direction != BindingMode.TwoWay)
                        throw new InvalidOperationException("TwoWay binding is not allowed for this binding.");
                    break;
                case BindingMode.ReadOnly:
                    if ((sourceAttribute.Direction != BindingMode.WriteOnly && sourceAttribute.Direction != BindingMode.TwoWay)
                    || (destAttribute.Direction != BindingMode.TwoWay && destAttribute.Direction != BindingMode.ReadOnly))
                        throw new InvalidOperationException("ReadOnly binding is not allowed for this binding.");
                    break;
                case BindingMode.WriteOnly:
                    if ((sourceAttribute.Direction != BindingMode.ReadOnly && sourceAttribute.Direction != BindingMode.TwoWay)
                    || (destAttribute.Direction != BindingMode.TwoWay && destAttribute.Direction != BindingMode.WriteOnly))
                        throw new InvalidOperationException("WriteOnly binding is not allowed for this binding.");
                    break;
            }

            var sourceBindingInformations = new BindingInformations
            {
                Instance = target,
                Property = target.GetType().GetProperty(destinationProperty),
                Direction = direction,
                Adapter = adapterToDestination
            };

            BindingMode invertedDirection = BindingMode.TwoWay;
            switch (direction)
            {
                case BindingMode.ReadOnly: invertedDirection = BindingMode.WriteOnly; break;
                case BindingMode.WriteOnly: invertedDirection = BindingMode.ReadOnly; break;
                case BindingMode.TwoWay: invertedDirection = BindingMode.TwoWay; break;
            }

            var destBindingInformations = new BindingInformations
            {
                Instance = this,
                Property = GetType().GetProperty(source),
                Direction = invertedDirection,
                Adapter = adapterToSource
            };

            SetPropagationVector(source, sourceBindingInformations);
            target.SetPropagationVector(destinationProperty, destBindingInformations);

            checkBindings(this, new System.ComponentModel.PropertyChangedEventArgs(source));
            target.checkBindings(this, new System.ComponentModel.PropertyChangedEventArgs(destinationProperty));
        }

        /// <summary>
        /// Remove a binding between a property of this class and a property of a
        /// target class.
        /// </summary>
        /// <param name="source">Source property.</param>
        /// <param name="target">Target <see cref="pObject"/> which have the target property.</param>
        /// <param name="destinationProperty">Target property.</param>
        public void Unbind(string source, pObject target, string destinationProperty)
        {
            var binding = propagationVectors[source].FindIndex(x => x.Instance == target && x.Property.Name == destinationProperty);

            if (binding < 0)
                throw new InvalidOperationException("Invalid binding.");

            var remote = target.propagationVectors[source].FindIndex(x => x.Instance == this && x.Property.Name == source);

            propagationVectors[source].RemoveAt(binding);
            target.propagationVectors[source].RemoveAt(remote);
        }

        #endregion

        #region Caching

        private struct BindingInformations
        {
            public pObject Instance;
            public PropertyInfo Property;
            public BindingMode Direction;
            public Func<object, object> Adapter;
            public Action Propagator;
        }

        private Dictionary<string, List<BindingInformations>> propagationVectors;

        private void SetPropagationVector(string prop, BindingInformations info)
        {
            if (!propagationVectors.ContainsKey(prop))
                propagationVectors.Add(prop, new List<BindingInformations>());

            var type = GetType();
            var property = type.GetProperty(prop);
            var propertyType = property.PropertyType;

            info.Propagator = () =>
            {
                var value = property.GetValue(this);

                switch (info.Direction)
                {
                    case BindingMode.ReadOnly: break;
                    case BindingMode.WriteOnly:
                    case BindingMode.TwoWay:
                        if (info.Adapter != null)
                            info.Property.SetValue(info.Instance, info.Adapter.Invoke(value));
                        else
                            info.Property.SetValue(info.Instance, value);
                        break;
                }
            };

            propagationVectors[prop].Add(info);
        }

        #endregion

        #region Binding update

        private void checkBindings(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (propagationVectors.ContainsKey(e.PropertyName))
            {
                foreach (var binding in propagationVectors[e.PropertyName])
                {
                    binding.Propagator.Invoke();
                }
            }
        }

        #endregion

    }
}