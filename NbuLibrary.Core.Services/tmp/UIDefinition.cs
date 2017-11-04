using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;
using Newtonsoft.Json.Linq;

namespace NbuLibrary.Core.Service.tmp
{
    //TODO: Move from here!!!
    public class JsonUIDefinitionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(UIDefinition).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            var typeId = Convert.ToInt32(jObject.Property("Type").Value.ToString());
            var type = (UITypes)typeId;

            object target = null;
            switch (type)
            {
                case UITypes.Form:
                    target = new FormDefinition();
                    break;
                case UITypes.Grid:
                    target = new GridDefinition();
                    break;
                case UITypes.View:
                    target = new ViewDefinition();
                    break;
                default:
                    throw new NotImplementedException("The UIDefinition type is not supported!");
            }

            serializer.Populate(jObject.CreateReader(), target);
            return target;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var prop in value.GetType().GetProperties())
            {
                writer.WritePropertyName(prop.Name);
                var ms = new System.IO.MemoryStream();
                var tw = new System.IO.StreamWriter(ms);
                serializer.Serialize(tw, prop.GetValue(value));
                tw.Flush();
                var res = new System.IO.StreamReader(ms).ReadToEnd();
                System.Diagnostics.Trace.WriteLine(res);

                serializer.Serialize(writer, prop.GetValue(value));
            }
            writer.WriteEndObject();
        }
    }

    public class JsonViewFieldConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ViewField).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            var typeId = Convert.ToByte(jObject.Property("Type").Value.ToString());
            var type = (ViewTypes)typeId;

            object target = null;
            switch (type)
            {
                case ViewTypes.Textfield:
                    target = new Textfield();
                    break;
                case ViewTypes.Numberfield:
                    target = new Numberfield();
                    break;
                case ViewTypes.Datefield:
                    target = new Datefield();
                    break;
                case ViewTypes.Enumfield:
                    target = new Enumfield();
                    break;
                case ViewTypes.Filefield:
                    target = new Filefield();
                    break;
                case ViewTypes.Htmlfield:
                    target = new Htmlfield();
                    break;
                default:
                    target = new ViewField();
                    break;
            }

            serializer.Populate(jObject.CreateReader(), target);
            return target;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var prop in value.GetType().GetProperties())
            {
                writer.WritePropertyName(prop.Name);
                serializer.Serialize(writer, prop.GetValue(value));
            }
            writer.WriteEndObject();
        }
    }

    public class JsonEditFieldConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(EditField).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            var typeId = Convert.ToByte(jObject.Property("Type").Value.ToString());
            var type = (EditTypes)typeId;

            object target = null;
            switch (type)
            {
                case EditTypes.Textbox:
                    target = new Textbox();
                    break;
                case EditTypes.Numberbox:
                    target = new Numberbox();
                    break;
                case EditTypes.Datepicker:
                    target = new Datepicker();
                    break;
                case EditTypes.Enumlist:
                    target = new Enumlist();
                    break;
                case EditTypes.Selectlist:
                    target = new Selectlist();
                    break;
                case EditTypes.FileUpload:
                    target = new FileUpload();
                    break;
                case EditTypes.Autocomplete:
                    target = new Autocomplete();
                    break;
                case EditTypes.Htmlbox:
                    target = new Htmlbox();
                    break;
                default:
                    target = new EditField();
                    break;
            }

            serializer.Populate(jObject.CreateReader(), target);
            return target;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var prop in value.GetType().GetProperties())
            {
                writer.WritePropertyName(prop.Name);
                serializer.Serialize(writer, prop.GetValue(value));
            }
            writer.WriteEndObject();
        }
    }

    //TODO: UIBuilders
    public class UIGridBuilder
    {
        public UIGridBuilder(string name, string entity, string label = null)
        {
            Grid = new GridDefinition() { Name = name, Label = label ?? name, Entity = entity };
        }

        public GridDefinition Grid { get; private set; }

        public void AddText(string property, string label)
        {
            Grid.Fields.Add(new Textfield() {
                Property = property,
                Label = label,
                Order = Grid.Fields.Count
            });
        }
    }

    [XmlRoot]
    public class UIDefinitionStore
    {
        public List<UIDefinition> Definitions { get; set; }

        public UIDefinitionStore()
        {
            Definitions = new List<UIDefinition>();
        }
    }

    public enum UITypes
    {
        Grid,
        Form,
        View
    }

    [JsonConverter(typeof(JsonUIDefinitionConverter))]
    [XmlInclude(typeof(GridDefinition)),
    XmlInclude(typeof(FormDefinition)),
    XmlInclude(typeof(ViewDefinition))]
    public class UIDefinition
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Label { get; set; }

        [XmlAttribute]
        public string Entity { get; set; }

        [XmlAttribute]
        public virtual UITypes Type { get; set; }
    }

    [XmlInclude(typeof(Textfield)),
    XmlInclude(typeof(Numberfield)),
    XmlInclude(typeof(Datefield)),
    XmlInclude(typeof(Enumfield))]
    public class GridDefinition : UIDefinition
    {

        [XmlArray]
        public List<ViewField> Fields { get; set; }

        //TODO:GridDefinition - Filters
        [XmlArray]
        public List<ViewField> Filters { get; set; }

        [XmlAttribute]
        public override UITypes Type
        {
            get
            {
                return UITypes.Grid;
            }
            set
            {
                base.Type = value;
            }
        }
    }

    [XmlInclude(typeof(Textbox)),
    XmlInclude(typeof(Htmlbox)),
    XmlInclude(typeof(Numberbox)),
    XmlInclude(typeof(Datepicker)),
    XmlInclude(typeof(Enumlist)),
    XmlInclude(typeof(Selectlist)),
    XmlInclude(typeof(Autocomplete)),
    XmlInclude(typeof(FileUpload))]
    public class FormDefinition : UIDefinition
    {
        [XmlArray]
        public List<EditField> Fields { get; set; }

        [XmlAttribute]
        public override UITypes Type
        {
            get
            {
                return UITypes.Form;
            }
            set
            {
                base.Type = value;
            }
        }
    }

    [JsonConverter(typeof(JsonEditFieldConverter))]
    public class EditField
    {
        public string Label { get; set; }
        public string Property { get; set; }
        public EditTypes Type { get; set; }
        public int Order { get; set; }

        public bool Required { get; set; }
    }

    public enum EditTypes
    {
        Textbox,
        Numberbox,
        Datepicker,
        Enumlist,
        Selectlist,
        Checkbox,
        FileUpload,
        Autocomplete,
        Htmlbox
    }

    public class Textbox : EditField
    {
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public bool Multiline { get; set; }
    }
    public class Htmlbox : EditField
    {
        public Htmlbox()
        {
            Type = EditTypes.Htmlbox;
        }

        public int MinLength { get; set; }
        public int MaxLength { get; set; }
    }

    public class Numberbox : EditField
    {
        public Numberbox()
        {
            Type = EditTypes.Numberbox;
        }

        public int? Min { get; set; }
        public int? Max { get; set; }
        public bool Integer { get; set; }
    }

    public class Datepicker : EditField
    {
        public Datepicker()
        {
            Type = EditTypes.Datepicker;
        }

        public DateTime? Min { get; set; }
        public DateTime? Max { get; set; }
        public bool? Future { get; set; }
        public int? DaysOffset { get; set; }
    }

    public class Enumlist : EditField
    {
        public Enumlist()
        {
            Type = EditTypes.Enumlist;
        }
        public string EnumClass { get; set; }
    }

    public class Selectlist : EditField
    {
        public Selectlist()
        {
            Type = EditTypes.Selectlist;
        }

        public string Entity { get; set; }
        public string Role { get; set; }
        public bool Multiple { get; set; }
        public string Formula { get; set; }
    }

    public class Autocomplete : EditField
    {
        public Autocomplete()
        {
            Type = EditTypes.Autocomplete;
        }
        public string Entity { get; set; }
        public string Role { get; set; }
        public bool Multiple { get; set; }
    }

    public class FileUpload : EditField
    {
        public FileUpload()
        {
            Type = EditTypes.FileUpload;
        }

        public string Entity { get; set; }
        public string Role { get; set; }
        public bool Multiple { get; set; }
    }

    //=========================================

    [XmlInclude(typeof(Textfield)),
    XmlInclude(typeof(Numberfield)),
    XmlInclude(typeof(Datefield)),
    XmlInclude(typeof(Enumfield)),
    XmlInclude(typeof(Filefield)),
    XmlInclude(typeof(Htmlfield))]
    public class ViewDefinition : UIDefinition
    {
        [XmlAttribute]
        public override UITypes Type
        {
            get
            {
                return UITypes.View;
            }
            set
            {
                base.Type = value;
            }
        }

        [XmlArray]
        public List<ViewField> Fields { get; set; }

        public ViewDefinition()
        {
            Fields = new List<ViewField>();
        }
    }

    [JsonConverter(typeof(JsonViewFieldConverter))]
    public class ViewField
    {
        [XmlAttribute]
        public string Label { get; set; }

        [XmlAttribute]
        public ViewTypes Type { get; set; }

        [XmlAttribute]
        public int Order { get; set; }

        [XmlAttribute]
        public string Property { get; set; }

        [XmlAttribute]
        public string Role { get; set; }

        [XmlAttribute]
        public string Entity { get; set; }


    }

    public enum ViewTypes
    {
        Textfield,
        Numberfield,
        Datefield,
        Enumfield,
        Checkfield,
        Listfield,
        Filefield,
        Htmlfield
    }

    public class Textfield : ViewField
    {
        public int Length { get; set; }
        public bool Multiline { get; set; }
    }

    public class Htmlfield : ViewField
    {
        public Htmlfield()
        {
            Type = ViewTypes.Htmlfield;
        }

        public int Length { get; set; }
    }

    public class Numberfield : ViewField
    {
        public Numberfield()
        {
            Type = ViewTypes.Numberfield;
        }
    }
     
    public class Datefield : ViewField
    {
        public Datefield()
        {
            Type = ViewTypes.Datefield;
        }
    }

    public class Enumfield : ViewField
    {
        public Enumfield()
        {
            Type = ViewTypes.Enumfield;
        }
        public string EnumClass { get; set; }
    }

    public class Filefield : ViewField
    {
        public Filefield()
        {
            Type = ViewTypes.Filefield;
        }
        public bool Multiple { get; set; }
    }
}