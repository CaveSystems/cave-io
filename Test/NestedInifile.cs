

using NUnit.Framework;

using Cave.IO;
using System;
using System.Globalization;
using Test;

namespace Tests.Cave.IO.IniFile;

[TestFixture]
public class NestedInifile
{
    #region Public Classes

    public class NestedRoot : IEquatable<NestedRoot>
    {
        #region Public Properties

        [IniIgnore]
        public object SomethingSpecial { get; set; }

        [IniSection]
        public SettingsObjectFields Sub1 { get; set; }

        [IniSection(Name = "Section2")]
        public SettingsStructFields Sub2 { get; set; }

        [IniSection(SettingsType = IniSettingsType.Properties)]
        public SettingsObjectProperties Sub3 { get; set; }

        [IniSection(Name = "Section4", SettingsType = IniSettingsType.Properties)]
        public SettingsStructProperties Sub4 { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static NestedRoot Random(CultureInfo culture)
        {
            var result = new NestedRoot
            {
                Sub1 = SettingsObjectFields.Random(culture),
                Sub2 = SettingsStructFields.Random(culture),
                Sub3 = SettingsObjectProperties.Random(culture),
                Sub4 = SettingsStructProperties.Random(culture),
                SomethingSpecial = Environment.TickCount / 7.31
            };
            return result;
        }

        public bool Equals(NestedRoot other) =>
            Equals(Sub1, other.Sub1) &&
            Equals(Sub2, other.Sub2) &&
            Equals(Sub3, other.Sub3) &&
            Equals(Sub4, other.Sub4);

        public override bool Equals(object obj) => Equals(obj as NestedRoot);

        public override int GetHashCode()
        {
            var hashCode = -146196424;
            hashCode = (hashCode * -1521134295) + Sub1.GetHashCode();
            hashCode = (hashCode * -1521134295) + Sub2.GetHashCode();
            hashCode = (hashCode * -1521134295) + Sub3.GetHashCode();
            hashCode = (hashCode * -1521134295) + Sub4.GetHashCode();
            hashCode = (hashCode * -1521134295) + SomethingSpecial.GetHashCode();
            return hashCode;
        }

        #endregion Public Methods
    }

    #endregion Public Classes

    #region Public Constructors

    static NestedInifile() => Program.Init();

    #endregion Public Constructors

    #region Public Methods

    [Test]
    public void NestedInifileTest()
    {
        var writer = new IniWriter();
        var test = NestedRoot.Random(CultureInfo.InvariantCulture);
        writer.WriteProperties("test", test);

        var reader = writer.ToReader();
        var current = reader.ReadObjectProperties<NestedRoot>("test");
        Assert.AreEqual(test, current);
    }

    #endregion Public Methods
}
