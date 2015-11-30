using System.Collections.Generic;
using System.Xml.Serialization;

namespace NLPUnpacker
{
    [XmlInclude(typeof(SPString))]
    [XmlInclude(typeof(SPInteger))]
    [XmlInclude(typeof(SPBoolean))]
    [XmlInclude(typeof(SPFloat))]
    [XmlInclude(typeof(SPStringArray))]
    [XmlInclude(typeof(SPIntegerArray))]
    [XmlInclude(typeof(SPBooleanArray))]
    [XmlInclude(typeof(SPFloatArray))]
    [XmlInclude(typeof(SPNestedArray))]
    public class SERIParameter
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Type;
    }

    public class SPString : SERIParameter
    {
        public string Value;

        public SPString(string Name, string Value)
        {
            this.Name = Name;
            Type = "String";
            this.Value = Value;
        }

        public SPString()
        {
        }
    }

    public class SPInteger : SERIParameter
    {
        public int Value;

        public SPInteger(string Name, int Value)
        {
            this.Name = Name;
            Type = "Integer";
            this.Value = Value;
        }

        public SPInteger()
        {
        }
    }

    public class SPBoolean : SERIParameter
    {
        public bool Value;

        public SPBoolean(string Name, bool Value)
        {
            this.Name = Name;
            Type = "Boolean";
            this.Value = Value;
        }

        public SPBoolean()
        {
        }
    }

    public class SPFloat : SERIParameter
    {
        public float Value;

        public SPFloat(string Name, float Value)
        {
            this.Name = Name;
            Type = "Float";
            this.Value = Value;
        }

        public SPFloat()
        {
        }
    }

    public class SPStringArray : SERIParameter
    {
        public string[] Values;

        public SPStringArray(string Name, string[] Values)
        {
            this.Name = Name;
            Type = "StringArray";
            this.Values = Values;
        }

        public SPStringArray()
        {
        }
    }

    public class SPIntegerArray : SERIParameter
    {
        public int[] Values;

        public SPIntegerArray(string Name, int[] Values)
        {
            this.Name = Name;
            Type = "IntegerArray";
            this.Values = Values;
        }

        public SPIntegerArray()
        {
        }
    }

    public class SPBooleanArray : SERIParameter
    {
        public bool[] Values;

        public SPBooleanArray(string Name, bool[] Values)
        {
            this.Name = Name;
            Type = "BooleanArray";
            this.Values = Values;
        }

        public SPBooleanArray()
        {
        }
    }

    public class SPFloatArray : SERIParameter
    {
        public float[] Values;

        public SPFloatArray(string Name, float[] Values)
        {
            this.Name = Name;
            Type = "FloatArray";
            this.Values = Values;
        }

        public SPFloatArray()
        {
        }
    }

    public class SPNestedArray : SERIParameter
    {
        [XmlArrayItem("Parameter")]
        public SERIParameter[] Values;

        public SPNestedArray(string Name, SERIParameter[] Values)
        {
            this.Name = Name;
            Type = "NestedArray";
            this.Values = Values;
        }

        public SPNestedArray()
        {
        }
    }

    [XmlRoot]
    public class SERI
    {
        [XmlArrayItem("Parameter")]
        public List<SERIParameter> Parameters = new List<SERIParameter>();

        public void Add(SERIParameter Parameter)
        {
            Parameters.Add(Parameter);
        }
    }
}
