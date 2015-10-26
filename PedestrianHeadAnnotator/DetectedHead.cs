using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PedestrianHeadAnnotator
{
    [System.Xml.Serialization.XmlRoot("root")]
    public class DetectedHead
    {
        #region XmlSerialization用クラス
        public class Frame
        {
            [System.Xml.Serialization.XmlAttribute("number")]
            public int Number { get; set; }
            [System.Xml.Serialization.XmlElement("objectlist")]
            public ObjectList ObjectList { get; set; } = new ObjectList();
        }
        public class ObjectList
        {
            [System.Xml.Serialization.XmlElement("object")]
            public List<Object> Objects { get; set; } = new List<Object>();
        }
        public class Object
        {
            [System.Xml.Serialization.XmlAttribute("id")]
            public int Id { get; set; }
            [System.Xml.Serialization.XmlElement("box")]
            public Box Box { get; set; } = new Box();
            [System.Xml.Serialization.XmlElement("body")]
            public Body Body { get; set; } = new Body();
        }
        public class Box
        {
            [System.Xml.Serialization.XmlAttribute("h")]
            public int Height { get; set; }
            [System.Xml.Serialization.XmlAttribute("w")]
            public int Width { get; set; }
            [System.Xml.Serialization.XmlAttribute("xc")]
            public int XCenter { get; set; }
            [System.Xml.Serialization.XmlAttribute("yc")]
            public int YCenter { get; set; }
        }
        public class Body
        {
            [System.Xml.Serialization.XmlElement("head")]
            public Head Head { get; set; } = new Head();
        }
        public class Head
        {
            [System.Xml.Serialization.XmlAttribute("gaze")]
            public int Gaze { get; set; }
            [System.Xml.Serialization.XmlAttribute("size")]
            public int Size { get; set; }
            [System.Xml.Serialization.XmlAttribute("xc")]
            public int XCenter { get; set; }
            [System.Xml.Serialization.XmlAttribute("yc")]
            public int YCenter { get; set; }
        }
        #endregion

        public static DetectedHead ReadXml(string filename)
        {
            DetectedHead detectedHead = null;
            using (System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Open))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(DetectedHead));
                detectedHead = (DetectedHead)serializer.Deserialize(fs);
            }
            return detectedHead;
        }
        public void WriteXml(string filename)
        {
            using(System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Create))
            {
                System.Xml.Serialization.XmlSerializer serialize = new System.Xml.Serialization.XmlSerializer(typeof(DetectedHead));
                serialize.Serialize(fs, this);
            }
        }
        [System.Xml.Serialization.XmlElement("frame")]
        public List<Frame> Frames { get; set; } = new List<Frame>();
    }
}
