using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace Phantom.Testing.TestAdapter.Settings
{
    public class DiscovererSettings
    {
        [XmlElement("UseWrapper")]
        public bool UseWrapper { get; set; }

        [XmlElement("Wrapper")]
        public string Wrapper { get; set; }
    }

    public class ReporterSettings
    {
        [XmlElement("UseWrapper")]
        public bool UseWrapper { get; set; }

        [XmlElement("Wrapper")]
        public string Wrapper { get; set; }
    }

    public class EnvironmentVariable
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Value")]
        public string Value { get; set; }
    }

    [XmlRoot(Constants.SettingsName)]
    public class TestingRunSettings : ITestingTestAdapterSettings
    {
        [XmlElement("DebuggingNamedPipeId")]
        public virtual string DebuggingNamedPipeId { get; set; }

        [XmlElement("Discoverer")]
        public virtual DiscovererSettings Discoverer { get; set; }

        [XmlElement("Reporter")]
        public virtual ReporterSettings Reporter { get; set; }

        [XmlElement("WatchdogDisabled")]
        public virtual bool? WatchdogDisabled { get; set; }

        [XmlElement("WorkingDirectory")]
        public virtual string WorkingDirectory { get; set; }

        [XmlElement("PathExtension")]
        public virtual string PathExtension { get; set; }

        [XmlArray("EnvironmentVariables")]
        [XmlArrayItem("Variable")]
        public virtual List<EnvironmentVariable> EnvironmentVariables { get; set; }

        public void FillMissingFrom(TestingRunSettings other)
        {
            if (other == null)
            {
                return;
            }

            DebuggingNamedPipeId = DebuggingNamedPipeId ?? other.DebuggingNamedPipeId;
            Discoverer = Discoverer ?? other.Discoverer;
            Reporter = Reporter ?? other.Reporter;
            WatchdogDisabled = WatchdogDisabled ?? other.WatchdogDisabled;
            WorkingDirectory = WorkingDirectory ?? other.WorkingDirectory;
            PathExtension = PathExtension ?? other.PathExtension;
            EnvironmentVariables = EnvironmentVariables ?? other.EnvironmentVariables;
        }

        public static TestingRunSettings LoadFromXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
            {
                return new TestingRunSettings();
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                return LoadFromXml(doc.CreateNavigator());
            }
            catch
            {
                return new TestingRunSettings();
            }
        }

        public static TestingRunSettings LoadFromXml(XPathNavigator navigator)
        {
            if (navigator == null)
            {
                return new TestingRunSettings();
            }

            try
            {
                var serializer = new XmlSerializer(typeof(TestingRunSettings));
                using (var reader = navigator.ReadSubtree())
                {
                    reader.MoveToContent();
                    var result = serializer.Deserialize(reader) as TestingRunSettings;
                    return result ?? new TestingRunSettings();
                }
            }
            catch
            {
                return new TestingRunSettings();
            }
        }

        public XmlElement ToXml()
        {
            var doc = new XmlDocument();
            using (XmlWriter writer = doc.CreateNavigator().AppendChild())
            {
                new XmlSerializer(typeof(TestingRunSettings)).Serialize(writer, this);
            }
            return doc.DocumentElement;
        }
    }
}
