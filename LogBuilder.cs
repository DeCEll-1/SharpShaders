using System.Text;

namespace SharpShaders
{
    internal class LogBuilder
    {
        public StringBuilder sb = new StringBuilder();
        public LogBuilder WriteLine(object text)
        {
            sb.AppendLine(text.ToString());
            return this;
        }
        public LogBuilder Write(object text)
        {
            sb.Append(text.ToString());
            return this;
        }
        public LogBuilder NewLine()
        {
            sb.AppendLine();
            return this;
        }

        public void Log()
        {
            Logger.Log(sb.ToString());
        }

    }
}
