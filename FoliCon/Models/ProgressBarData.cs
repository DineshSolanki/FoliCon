namespace FoliCon.Models
{
    public class ProgressBarData : BindableBase
    {
        private int _value;
        private int _max;
        private string _text;
        public int Value { get => _value; set => SetProperty(ref _value, value); }
        public int Max { get => _max; set => SetProperty(ref _max, value); }
        public string Text { get => _text; set => SetProperty(ref _text, value); }
    }
}