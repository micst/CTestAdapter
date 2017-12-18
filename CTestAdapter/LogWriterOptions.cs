namespace CTestAdapter
{ 
  public struct LogWriterOptions
  {
    public LogLevel CurrentLogLevel { get; set; }

    public bool EnableLogFile { get; set; }

    public bool AppendToLogFile { get; set; }

    public string LogFileName { get; set; }

    public static bool operator ==(LogWriterOptions o1, LogWriterOptions o2)
    {
      return o1.Equals(o2);
    }

    public static bool operator !=(LogWriterOptions o1, LogWriterOptions o2)
    {
      return !o1.Equals(o2);
    }

    public override bool Equals(object obj)
    {
      if (null == obj)
      {
        return false;
      }
      var opts = (LogWriterOptions) obj;
      return opts.EnableLogFile == this.EnableLogFile &&
             opts.AppendToLogFile == this.AppendToLogFile &&
             opts.CurrentLogLevel == this.CurrentLogLevel &&
             opts.LogFileName == this.LogFileName;
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }
  }
}
