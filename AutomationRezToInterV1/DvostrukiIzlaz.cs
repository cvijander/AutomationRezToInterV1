using System;
using System.IO;
using System.Text;

namespace AutomationRezToInterV1
{
    // Sve što ide u konzolu, ide i u log fajl (sa vremenom za svaku liniju)
    public class DvostrukiIzlaz : TextWriter
    {
        private readonly TextWriter _konzola;
        private readonly StreamWriter _fajl;

        public DvostrukiIzlaz(TextWriter konzola, string putanja)
        {
            _konzola = konzola;
            _fajl = new StreamWriter(putanja, append: true, Encoding.UTF8) { AutoFlush = true };
        }

        public override Encoding Encoding => _konzola.Encoding;

        public override void Write(char value) { _konzola.Write(value); _fajl.Write(value); }
        public override void Write(string value) { _konzola.Write(value); _fajl.Write(value); }

        public override void WriteLine(string value)
        {
            _konzola.WriteLine(value);
            _fajl.WriteLine($"{DateTime.Now:HH:mm:ss.fff}  {value}");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _fajl.Dispose();
            base.Dispose(disposing);
        }
    }
}