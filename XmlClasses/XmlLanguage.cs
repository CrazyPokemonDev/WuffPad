using System.Xml.Serialization;

namespace XMLClasses
{
    public class XmlLanguage
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("base")]
        public string Base { get; set; }
        [XmlAttribute("variant")]
        public string Variant { get; set; }
        [XmlAttribute("owner")]
        public string Owner { get; set; }
        [XmlAttribute("code")]
        public string Code { get; set; }
        [XmlAttribute("isDefault")]
        private string IsDefaultString { get; set; }
        [XmlIgnore]
        public bool IsDefault
        {
            get
            {
                return IsDefaultString == "true";
            }
            set
            {
                IsDefaultString = value ? "true" : "false";
            }
        }
    }
}
