namespace Models
{
    public class Setting : Extension
    {
        public int Type { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public int ValueType { get; set; }

        public bool IsCode { get; set; } = false;

        public bool IsChoose { get; set; } = false; // Use edit, delete rule
    }
}
