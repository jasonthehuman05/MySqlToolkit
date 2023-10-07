namespace MySqlToolkit
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class MySqlColumn : Attribute
    {
        private string _name = "";
        private bool _isAuto;

        public string Name { get => _name; set => _name = value; }
        public bool IsAuto { get => _isAuto; set => _isAuto = value; }

        public MySqlColumn(string name, bool isAuto)
        {
            _name = name;
            _isAuto = isAuto;
        }
    }
}
