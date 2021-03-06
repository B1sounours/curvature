﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using Curvature.Widgets;

namespace Curvature
{

    class AgentPropertyAdapter : ICustomTypeDescriptor
    {
        private Dictionary<KnowledgeBase.Record, object> PropertyDict;

        public AgentPropertyAdapter(Dictionary<KnowledgeBase.Record, object> propdict)
        {
            PropertyDict = propdict;
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return PropertyDict;
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return null;
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return ((ICustomTypeDescriptor)this).GetProperties(new Attribute[0]);
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            var properties = new List<PropertyDescriptor>();
            foreach (var kvp in PropertyDict)
            {
                if (kvp.Key.Params == KnowledgeBase.Record.Parameterization.Enumeration)
                    properties.Add(new DictionaryPropertyDescriptor<string>(PropertyDict, kvp.Key));
                else
                    properties.Add(new DictionaryPropertyDescriptor<double>(PropertyDict, kvp.Key));
            }

            return new PropertyDescriptorCollection(properties.ToArray());
        }
    }

    [TypeConverter(typeof(KBPropertyConverter))]
    [Editor(typeof(KBPropertyEditor), typeof(UITypeEditor))]
    class KBPropertyConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type t)
        {
            if (t == typeof(string))
                return true;

            return base.CanConvertFrom(context, t);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                if (double.TryParse(value as string, out double result))
                   return result;

                return value;
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
                return value.ToString();

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    class KBPropertyWrapper<T>
    {
        Dictionary<KnowledgeBase.Record, object> PropertyDict;
        internal KnowledgeBase.Record Record;


        internal KBPropertyWrapper(Dictionary<KnowledgeBase.Record, object> dict, KnowledgeBase.Record key)
        {
            PropertyDict = dict;
            Record = key;
        }

        internal void SetProperty(object value)
        {
            PropertyDict[Record] = value;
        }

        internal T GetProperty()
        {
            return (T)PropertyDict[Record];
        }

        public override string ToString()
        {
            return $"{PropertyDict[Record]}";
        }
    }


    class KBPropertyEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider == null)
                return value;

            var editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            if (editorService == null)
                return value;

            var wrapper = value as KBPropertyWrapper<double>;
            if (wrapper == null)
            {
                var stringwrap = value as KBPropertyWrapper<string>;
                if (stringwrap == null)
                    return value;


                var listcontrol = new System.Windows.Forms.ListBox();
                foreach (var str in stringwrap.Record.EnumerationValues)
                    listcontrol.Items.Add(str);

                listcontrol.SelectedItem = stringwrap.GetProperty();
                listcontrol.Click += (obj, args) =>
                {
                    editorService.CloseDropDown();
                };

                editorService.DropDownControl(listcontrol);

                if (listcontrol.SelectedIndex >= 0)
                    value = listcontrol.SelectedItem;

                return value;
            }

            var control = new EditWidgetKnowledgeBaseParameter(wrapper.GetProperty(), wrapper.Record.MinimumValue, wrapper.Record.MaximumValue);

            editorService.DropDownControl(control);

            value = control.GetValue();


            return value;
        }
    }


    class DictionaryPropertyDescriptor<T> : PropertyDescriptor
    {
        KBPropertyWrapper<T> Wrap;

        internal DictionaryPropertyDescriptor(Dictionary<KnowledgeBase.Record, object> dict, KnowledgeBase.Record key)
            : base(key.ReadableName, null)
        {
            Wrap = new KBPropertyWrapper<T>(dict, key);
        }

        public override Type PropertyType
        {
            get { return typeof(KBPropertyConverter); }
        }

        public override void SetValue(object component, object value)
        {
            Wrap.SetProperty(value);
        }

        public override object GetValue(object component)
        {
            return Wrap;
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type ComponentType
        {
            get { return null; }
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override void ResetValue(object component)
        {
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public override string Category => "Knowledge Base";
    }
}
